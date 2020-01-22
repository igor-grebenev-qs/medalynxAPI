using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;

namespace Medalynx {
    public class Api {
        // Extract value of query parameter as string
        public string GetValue(HttpContext context, string key)
        {
            if (!context.Request.Query.Keys.Contains(key))
            {
                return "";
            }

            var queryString = context.Request.Query;
            Microsoft.Extensions.Primitives.StringValues someValue;
            queryString.TryGetValue(key, out someValue);

            return someValue[0];
        }

        private Guid ForEmptyGuid(bool canCreate) {
            if (canCreate) {return Guid.NewGuid();}
            else {return new Guid();}
        }

        //Create guid from parameter (or new)
        private Guid ToGuid (string stringGuid, bool canCreate = true) {
            try
            {
                Guid newGuid = Guid.Parse(stringGuid);
                return newGuid;
            }
            catch (ArgumentNullException)
            {
                return ForEmptyGuid(canCreate);
            }
            catch (FormatException)
            {
                return ForEmptyGuid(canCreate);
            }
        }

        private object QueryArgumentsToObject(HttpContext context, object obj) {
            foreach (string key in context.Request.Query.Keys) {
                try
                {
                    // Get the Type object corresponding to MyClass.
                    Type objType=obj.GetType();
                    // Get the PropertyInfo object by passing the property name.
                    PropertyInfo myPropInfo = objType.GetProperty(key);
                    // Set value to object
                    var propType = myPropInfo.PropertyType;
                    if (propType.Equals(typeof(string)))
                    {
                        myPropInfo.SetValue(obj, this.GetValue(context, key));
                    }
                    else if (propType.Equals(typeof(int)))
                    {
                        myPropInfo.SetValue(obj, Int32.Parse(this.GetValue(context, key)));
                    }
                    else
                    {
                        new Exception("Not supported type. todo.");
                    }
                }
                catch(NullReferenceException e)
                {
                    Console.WriteLine("The property does not exist in User class." + e.Message);
                }
            }
            return obj;
        }

        public class UserApi : Api{
            public System.Threading.Tasks.Task AddUser(HttpContext context) {
                try
                {
                    using (MedialynxDbContext db = new MedialynxDbContext())
                    {
                        Guid id = ToGuid(this.GetValue(context, "Id")); // ability to create user with specified id
                        User newUser = new User { Id=id.ToString("B") };
                        QueryArgumentsToObject(context, newUser);
                        db.Users.Add(newUser);
                        db.SaveChanges();
                    }
                    return context.Response.WriteAsync("user successfuly added");
                }
                catch (Exception e) {
                    return context.Response.WriteAsync("user can't be added. " + e);
                }
            }
            
            public System.Threading.Tasks.Task UpdateUser(HttpContext context) {
                using (MedialynxDbContext db = new MedialynxDbContext())
                {
                    Guid id = ToGuid(this.GetValue(context, "Id"), false);
                    if (id != Guid.Empty)
                    {
                        string sid = id.ToString("B");
                        var user = db.Users.FirstOrDefault(user => user != null && user.Id == sid);
                        // Validate instance is not null
                        if (user != null)
                        {
                            try
                            {
                                // user exists. We can update it
                                QueryArgumentsToObject(context, user);
                                db.Users.Update(user);
                                db.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                return context.Response.WriteAsync("user can't be updated. " + e);
                            }
                        }
                        else
                        {
                            return context.Response.WriteAsync("user not found");            
                        }
                    }
                    else
                    {
                        return context.Response.WriteAsync("user with specified id not found");            
                    }
                }
                return context.Response.WriteAsync("user successfuly updated");
            }

            public System.Threading.Tasks.Task RemoveUser(HttpContext context) {
                using (MedialynxDbContext db = new MedialynxDbContext())
                {
                    Guid id = ToGuid(this.GetValue(context, "Id"), false);
                    if (id != Guid.Empty)
                    {
                        string sid = id.ToString("B");
                        var user = db.Users.FirstOrDefault(user => user != null && user.Id == sid);
                        // Validate instance is not null
                        if (user != null)
                        {
                            try
                            {
                                db.Users.Remove(user);
                                db.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                return context.Response.WriteAsync("user can't be removed. " + e);
                            }
                        }
                        else
                        {
                            return context.Response.WriteAsync("can't remove user with specified id " + sid + ". User not found.");
                        }
                    }
                }
                return context.Response.WriteAsync("user removed successfuly");
            }
            public System.Threading.Tasks.Task GetUser(HttpContext context) {
                try
                {
                    List<User> users = new List<User>();
                    using (MedialynxDbContext db = new MedialynxDbContext())
                    {
                        Guid id = ToGuid(this.GetValue(context, "Id"), false);
                        if (id != Guid.Empty)
                        {
                            string sid = id.ToString("B");
                            var user = db.Users.FirstOrDefault(user => user != null && user.Id == sid);
                            // Validate instance is not null
                            if (user != null)
                            {
                                users.Add(user);
                            }
                        }
                        else 
                        { // Add all users
                            users.AddRange(db.Users);
                        }
                    }
                    var json = JsonSerializer.Serialize(users);
                    return context.Response.WriteAsync(json);
                }
                catch (Exception e)
                {
                    return context.Response.WriteAsync("can't recive user(s). Exception occured. " + e);
                }
            }
        }
    }
}

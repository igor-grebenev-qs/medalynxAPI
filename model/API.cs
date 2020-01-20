using System;
using System.Linq;
using System.Reflection;
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

        public class UserApi : Api{
            public System.Threading.Tasks.Task AddUser(HttpContext context) {
                using (MedialynxDbContext db = new MedialynxDbContext())
                {
                    // recive query parameters
                    Guid newGuid = ToGuid(base.GetValue(context, "Id"));
                    string name = this.GetValue(context, "Name");
                    int age = int.Parse(this.GetValue(context, "Age"));

                    // New users
                    User newUser = new User { Id=newGuid.ToString("B"), Name = name, Age = age };

                    db.Users.Add(newUser);
                    db.SaveChanges();
                    
                    // var users = db.Users.ToList(); // userst todo research
                }
                return context.Response.WriteAsync("user added");
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
                            // user exists. We can update it
                            foreach (string key in context.Request.Query.Keys) {
                                try
                                    {
                                        // Get the Type object corresponding to MyClass.
                                        Type userType=typeof(User);       
                                        // Get the PropertyInfo object by passing the property name.
                                        PropertyInfo myPropInfo = userType.GetProperty(key);
                                        // Set value to object
                                        var propType = myPropInfo.PropertyType;
                                        if (propType.Equals(typeof(string)))
                                        {
                                            myPropInfo.SetValue(user, this.GetValue(context, key));
                                        }
                                        else if (propType.Equals(typeof(int)))
                                        {
                                            myPropInfo.SetValue(user, Int32.Parse(this.GetValue(context, key)));
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
                            db.Users.Update(user);
                            db.SaveChanges();
                        }
                    }
                }
                return context.Response.WriteAsync("user updated");
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
                            db.Users.Remove(user);
                            db.SaveChanges();
                        }
                        else {throw new Exception("Can't remove user with specified id " + sid);}
                    }
                }
                return context.Response.WriteAsync("user removed successfuly");
            }
            public System.Threading.Tasks.Task GetUser(HttpContext context) {
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
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication2
{
    public class Program
    {
        //public static string deserializationError = "no";

        public static void Main()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();
            var virgin = true;
            app.Run(async (context) =>
            {
                var response = context.Response;
                response.Headers.ContentLanguage = "ru-RU";
                response.ContentType = "text/html; charset=utf-8";
                context.Response.StatusCode = 200;
                switch (context.Request.Path)
                {
                    case "/date":
                        await context.Response.WriteAsync($"Date: {DateTime.Now.ToShortDateString()}");
                        break;
                    case "/time":
                        await context.Response.WriteAsync($"Time: {DateTime.Now.ToShortTimeString()}");
                        break;
                    case "/api":
                        var responseText = "Incorrect data";
                        if (context.Request.HasJsonContentType())
                        {
                            var jsonoptions = new JsonSerializerOptions();
                            jsonoptions.Converters.Add(new PersonConverter());
                            try
                            {
                                var person = await context.Request.ReadFromJsonAsync<Person>(jsonoptions);
#pragma warning disable CS8602
                                responseText = $"Name: \"{person.Name}\"; Age: {person.Age}";
#pragma warning restore CS8602
                            }
                            catch (Exception e)
                            {
                                responseText = "Incorrect data";
                                Console.WriteLine($"PersonConvertException: {e.Message}");
                            }
                        }
                        await response.WriteAsJsonAsync(new { text = responseText });
                        break;
                    case "/":
                        if (virgin)
                        {
                            virgin = false;
                            context.Response.Redirect("https://127.0.0.1:443/", true);
                        }
                        else
                        {
                            response.ContentType = "text/html; charset=utf-8";
                            await context.Response.SendFileAsync("html/index.html");
                        }
                        break;
                    case "/download":
                        context.Response.Headers.ContentDisposition = "attachment; filename=3.png";
                        await context.Response.SendFileAsync("E:\\3.png");
                        break;
                    case "/redirect":
                        context.Response.Redirect("https://metanit.com/sharp/aspnet6/2.9.php", true);
                        break;
                    case "/postuser":
                        var langList = "";
                        foreach (var lang in context.Request.Form["languages"])
                        {
                            langList += $" {lang}";
                        }
                        await context.Response.WriteAsync($"<div><p>Name: {context.Request.Form["name"]}</p>" +
                        $"<p>Age: {context.Request.Form["age"]}</p>" +
                        $"<div>Languages:{langList}</ul></div>");
                        break;
                    default:
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync($"404 - Not found: \"{context.Request.Path}\".");
                        break;
                }
            });
            app.Run();
        }

        public record Person(string Name, byte Age);

        public class PersonConverter : JsonConverter<Person>
        {
            public override Person? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var personName = "Undefined";
                long personAge = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();
                        switch (propertyName?.ToLower())
                        {
                            case "name":
                                string? name = reader.GetString();
                                if (string.IsNullOrEmpty(name))
                                    throw new Exception("name is null or empty");
                                else
                                    personName = name;
                                break;
                            case "age" when reader.TokenType == JsonTokenType.String:
                                string? stringValue = reader.GetString();
                                if (string.IsNullOrEmpty(stringValue))
                                    throw new Exception("string age is null or empty");
                                else
                                    if (long.TryParse(stringValue, out long value))
                                    personAge = value;
                                break;
                            case "age" when reader.TokenType == JsonTokenType.Number:
                                personAge = reader.GetInt64();
                                break;
                            default:
                                throw new Exception("Unsupported JsonTokenType: \"{reader.TokenType}\".");
                        }
                    }
                    else
                        throw new Exception($"reader.TokenType = {reader.TokenType}");
                }
                if ((personAge < 0) || (personAge > 123))
                    throw new Exception("Not between");
                return new Person(personName, (byte)personAge);
            }

            public override void Write(Utf8JsonWriter writer, Person person, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("name", person.Name);
                writer.WriteNumber("age", person.Age);
                writer.WriteEndObject();
            }
        }
    }
}
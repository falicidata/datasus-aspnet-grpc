using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SusGrpc
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        IConfigurationRoot Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
            services.AddGrpc();
            services.AddResponseCompression();
            services.Configure<GzipCompressionProviderOptions>(cfg => cfg.Level = System.IO.Compression.CompressionLevel.Optimal);
        }

        private ProjectionDefinition<BsonDocument> GetIncludes(string campos)
        {

            var projection = Builders<BsonDocument>.Projection.Exclude("_id");
            if (!campos.Any()) return projection;
            foreach (var field in campos.Split(','))
            {
                projection = projection.Include(field);
            }
            return projection;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

          //  app.UseResponseCompression();

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapGet("/sus", async context =>
                {
                    context.Response.ContentType = "application/json";

                    try
                    {

                        var ufs = context.Request.Query.FirstOrDefault(q => q.Key == "ufs").Value.ToString();
                        var anos = context.Request.Query.FirstOrDefault(q => q.Key == "anos").Value.ToString();
                        var meses = context.Request.Query.FirstOrDefault(q => q.Key == "meses").Value.ToString();
                        var modulo = context.Request.Query.FirstOrDefault(q => q.Key == "modulo").Value.ToString();
                        var campos = context.Request.Query.FirstOrDefault(q => q.Key == "campos").Value.ToString();

                        MongoUrl mongoUrl = new MongoUrl(Configuration.GetConnectionString("MongoDB"));
                        MongoClient client = new MongoClient(mongoUrl);
                        IMongoDatabase database = client.GetDatabase("sus");
                        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(modulo.ToString().ToUpper());
                        ProjectionDefinitionBuilder<BsonDocument> includes = Builders<BsonDocument>.Projection;

                        var UfFilter = !ufs.Any() ? Builders<BsonDocument>.Filter.Empty : Builders<BsonDocument>.Filter.In("uf", ufs.Split(','));
                        var anoFilter = !anos.Any() ? Builders<BsonDocument>.Filter.Empty : Builders<BsonDocument>.Filter.In("ano", anos.Split(','));
                        var mesesFilter = !meses.Any() ? Builders<BsonDocument>.Filter.Empty : Builders<BsonDocument>.Filter.In("mes", meses.Split(','));
                        var find = collection.Find(UfFilter & anoFilter & mesesFilter);

                        long limit = 50000;

                        long total = await find.CountDocumentsAsync();

                        decimal pages = Math.Ceiling(decimal.Parse(total.ToString()) / decimal.Parse(limit.ToString()));
                        pages = pages < 1 ? 1 : pages;
                        await context.Response.WriteAsync("{\"success\": true, \"total\": " + total + ", \"data\": ");

                        await context.Response.WriteAsync("[");

                        for (int i = 1; i <= pages; i++)
                        {
                            var res = find.Skip((i - 1) * int.Parse(limit.ToString()))
                                            .Limit(int.Parse(limit.ToString()))
                                            .Project(GetIncludes(campos))
                                            .ToList().ToJson(new MongoDB.Bson.IO.JsonWriterSettings() { Indent = false , OutputMode = MongoDB.Bson.IO.JsonOutputMode.CanonicalExtendedJson});
                            

                            res = res.Replace("[", string.Empty).Replace("]", string.Empty);
                            res.Trim();
                            await context.Response.WriteAsync(i == pages ? res: $"{res},");

                            GC.Collect();
                        }

                        await context.Response.WriteAsync("]");
                        await context.Response.WriteAsync("}");
                    }
                    catch (Exception e)
                    {

                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, errors = new string[] { e.Message } }));
                        return;
                    }
                });
            });
        }
    }
}




//for (int i = 0; i <= cores; i++)
//{
//    pageCoreCountEnd = pageCoreCountStart == 1 ? pageCoreCountStart + (div-1): pageCoreCountStart + div ;
//    if (pageCoreCountEnd > pages) pageCoreCountEnd = int.Parse(pages.ToString());

//   tasks.Add(Task.Run(async () => await WriteMongo(collection, context, pageCoreCountStart, pageCoreCountEnd, limit, total)));

//    pageCoreCountStart = pageCoreCountEnd + 1;

//    if(pageCoreCountStart >= pages) break;

//}
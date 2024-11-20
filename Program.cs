using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace HW_ASP_4._2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();

            // Route to load image
            app.MapPost("/upload", async (HttpRequest request, IWebHostEnvironment env) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Invalid form data." });
                }

                var form = await request.ReadFormAsync();
                var file = form.Files["image"];

                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "No image provided." });
                }

                // Check file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    return Results.BadRequest(new { error = "Invalid image format." });
                }

                if (!allowedMimeTypes.Contains(file.ContentType))
                {
                    return Results.BadRequest(new { error = "Invalid image MIME type." });
                }

                // File size limit (eg up to 5 MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Results.BadRequest(new { error = "File size exceeds the limit of 5MB." });
                }

                // Generate a unique file name
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                // Path to save the file
                var uploadsFolder = Path.Combine(env.WebRootPath, "images");
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Create a folder if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Read file into byte array
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    // Save file to disk
                    await File.WriteAllBytesAsync(filePath, fileBytes);
                }

                // Generate URL to access image
                var imageUrl = $"{request.Scheme}://{request.Host}/images/{uniqueFileName}";

                return Results.Ok(new { url = imageUrl });
            })
            .WithName("UploadImage")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(200)
            .Produces(400);

            app.Run();
        }
    }
}

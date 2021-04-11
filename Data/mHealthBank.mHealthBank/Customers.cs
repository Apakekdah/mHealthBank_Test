using mHealthBank.Entities;
using mHealthBank.Models.Configuration;
using mHealthBank.Models.Forms;
using Microsoft.Extensions.Options;
using SAGE.Business;
using SAGE.Core.Commons;
using SAGE.Core.Interface;
using SAGE.IoC;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mHealthBank.Business
{
    public class Customers : BusinessClassBaseAsync<Customer>
    {
        private readonly IDisposableIoC life;
        private readonly CustomerConfiguration customerConfig;

        public ILogger Log { get; }

        public Customers(IRepositoryAsync<Customer> repository, IUnitOfWorkAsync unitofwork, IDisposableIoC life)
            : base(repository, unitofwork)
        {
            this.life = life;

            customerConfig = life.GetInstance<IOptions<CustomerConfiguration>>().Value;

            Log = life.GetInstance<ILogger>(new KeyValueParameter("type", this.GetType()));
        }

        public override Task Add(Customer entity)
        {
            Log.Info($"Adding Customer '{entity.CustomerName}'");

            if (!entity.KtpImage.IsNullOrEmpty())
            {
                var freeName = GetName(entity.KtpImage);

                Log.Info($"Writing file for '{entity.CustomerName}' with temporary name '{freeName}' Into '{customerConfig.BasePath}'");

                entity.KtpImage = Path.GetFileName(freeName);

                FileInfo fi = new FileInfo(freeName);
                using (var fs = fi.Create())
                {
                    entity.KtpImageStream.Seek(0, SeekOrigin.Begin);

                    var buff = new byte[2048];
                    var len = 0;

                    while ((len = entity.KtpImageStream.Read(buff, 0, buff.Length)) > 0)
                    {
                        fs.Write(buff, 0, len);
                    }
                    fs.Flush();
                }
            }
            return base.Add(entity);
        }

        public override async Task Update(Customer entity)
        {
            Log.Info($"Updating Customer '{entity.CustomerName}'");

            if (!entity.KtpImage.IsNullOrEmpty())
            {
                var custPrev = await GetById(entity.Id).ConfigureAwait(false);

                if (custPrev == null)
                    throw new Exception("Customer not found");

                if (!custPrev.KtpImage.IsNullOrEmpty())
                {
                    var freeName = custPrev.KtpImage;
                    try
                    {
                        CleanUpFailedThrow(custPrev.KtpImage);
                        entity.KtpImage = freeName;
                    }
                    catch
                    {
                        freeName = GetName(entity.KtpImage);
                        entity.KtpImage = Path.GetFileName(freeName);
                    }

                    Log.Info($"Replace file for '{entity.CustomerName}' with temporary name '{freeName}' Into '{customerConfig.BasePath}'");

                    FileInfo fi = new FileInfo(freeName);
                    using (var fs = fi.Create())
                    {
                        entity.KtpImageStream.Seek(0, SeekOrigin.Begin);

                        var buff = new byte[2048];
                        var len = 0;

                        while ((len = entity.KtpImageStream.Read(buff, 0, buff.Length)) > 0)
                        {
                            fs.Write(buff, 0, len);
                        }

                        fs.Flush();
                    }
                }
            }
            await base.Update(entity).ConfigureAwait(false);
        }

        public async Task<ImageInfo> GetImage(string id)
        {
            Log.Info($"Getting Image for CustomerID '{id}'");

            if (id.IsNullOrEmpty())
                throw new ArgumentException("id");
            var customer = await GetById(id);
            if (customer == null)
                throw new Exception("Customer not found");

            Log.Info($"Read file for '{customer.CustomerName}' with temporary name '{customer.KtpImage}' From '{customerConfig.BasePath}'");

            FileInfo fi = new FileInfo(Path.Combine(customerConfig.BasePath, customer.KtpImage));
            if (!fi.Exists)
                throw new FileNotFoundException();
            var ms = new MemoryStream();
            using (var fs = fi.Open(FileMode.Open))
            {
                await fs.CopyToAsync(ms).ConfigureAwait(false);
            }
            ms.Seek(0, SeekOrigin.Begin);

            var contentType = customer.KtpImage.GetContentType();

            Log.Info($"File Readed for '{customer.CustomerName}' with temporary name '{customer.KtpImage}' with content-type '{contentType}'");

            return new ImageInfo()
            {
                ContentType = contentType,
                Stream = ms
            };
        }

        public override async Task Delete(string id)
        {
            Log.Info($"Deleting CustomerID '{id}'");

            var customer = await GetById(id).ConfigureAwait(false);
            if (customer == null)
                throw new Exception("Customer not found");

            if (!customer.KtpImage.IsNullOrEmpty())
            {
                Log.Info($"Delete file for '{customer.CustomerName}' with temporary name '{customer.KtpImage}' From '{customerConfig.BasePath}'");

                try
                {
                    CleanUpFailedThrow(customer.KtpImage);
                }
                catch { }
            }
            await base.Delete(id);
        }

        public Task CleanUpFailed(string tempFile)
        {
            return Task.Run(() =>
            {
                Log.Info($"CleanUp Failed Customer Image with file '{tempFile}'");

                try
                {
                    CleanUpFailedThrow(tempFile);
                }
                catch { }
            });
        }

        private void CleanUpFailedThrow(string tempFile)
        {
            File.Delete(Path.Combine(customerConfig.BasePath, tempFile));
        }

        private string GetName(string sourceName)
        {
            var ext = Path.GetExtension(sourceName);

            if (!customerConfig.AllowedExtension.Any(c => c.Equals(ext)))
                throw new NotSupportedException($"Extension with '{ext}' not supported");

            string path;
            while (!File.Exists((path = Path.Combine(customerConfig.BasePath, RandomName(ext)))))
            {
                break;
            }
            return path;
        }

        private string RandomName(string ext)
        {
            return Path.ChangeExtension(DateTime.Now.ToString("yyyyMMddHHmmssfff"), ext);
        }
    }
}
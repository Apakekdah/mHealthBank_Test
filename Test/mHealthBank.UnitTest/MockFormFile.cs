using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace mHealthBank.UnitTest
{
    public class MockFormFile : IFormFile
    {
        FileInfo fi;

        public MockFormFile(string fileName)
        {
            fi = new FileInfo(fileName);
        }

        public string ContentType => GetContentType();

        public string ContentDisposition => throw new NotImplementedException();

        public IHeaderDictionary Headers => throw new NotImplementedException();

        public long Length => fi.Length;

        public string Name => fi.Name;

        public string FileName => fi.Name;

        public void CopyTo(Stream target)
        {
            throw new NotImplementedException();
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Stream OpenReadStream()
        {
            throw new NotImplementedException();
        }

        private string GetContentType()
        {
            string contentType;

            var ctProv = new FileExtensionContentTypeProvider();
            if (ctProv.TryGetContentType(fi.FullName, out contentType))
                contentType = "application/octed-stream";
            return contentType;
        }
    }
}
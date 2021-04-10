using System.IO;

namespace mHealthBank.Models.Forms
{
    public class ImageInfo
    {
        public string ContentType { get; set; }
        public MemoryStream Stream { get; set; }
    }
}

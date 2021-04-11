using mHealthBank.Interfaces.IO;
using System.IO;

namespace mHealthBank.System.IO
{
    public class SystemFileInfo : IFileInfo
    {
        public SystemFileInfo(string fileName)
        {
            FileInfo = new FileInfo(fileName);
        }

        public FileInfo FileInfo { get; }
    }
}
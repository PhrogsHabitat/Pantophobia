// I have not been able to get this to work, so I am commenting it out for now.
// This was so that we can convert a PhobiaModel (which is typically a .gbl) into a format unity can use.
// This way, we could stick to a singleton model loader and not have to worry about all the extra shit in the other formats

// ====================

// using System.IO;
// using System.Threading.Tasks;

// public class FileDownloadProvider : IDownloadProvider
// {
//     private readonly string _filePath;

//     public FileDownloadProvider(string filePath)
//     {
//         _filePath = filePath;
//     }

//     public async Task<IDownload> Request(Uri url)
//     {
//         if (File.Exists(_filePath))
//         {
//             byte[] data = File.ReadAllBytes(_filePath);
//             return new FileDownload(data);
//         }
//         return null;
//     }

//     public class FileDownload : IDownload
//     {
//         private readonly byte[] _data;

//         public FileDownload(byte[] data)
//         {
//             _data = data;
//         }

//         public bool Success => true;
//         public string Error => null;
//         public byte[] Data => _data;
//         public string Text => System.Text.Encoding.UTF8.GetString(_data);
//         public bool? IsBinary => true;
//     }
// }
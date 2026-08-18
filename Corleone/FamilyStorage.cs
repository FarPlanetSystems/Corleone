namespace Corleone
{
    public class FamilyStorage
    {
        public FamilyStorage() { }

        public void AddMemberStorage(string username)
        {

            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "FileStorage", username);
            Directory.CreateDirectory(path);
            Console.WriteLine($"Directory on the path {path} created");
        }

        public bool RemoveFromStorage(string memberName, string fileName)
        {
            try
            {
                string memberStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "FileStorage", memberName);
                string filePath = Path.Combine(memberStoragePath, fileName);
                Console.WriteLine($"File {fileName} removed from storage");
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File {fileName} not removed from storage");
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public async Task DownloadMemberFiles(string username, IFormFileCollection files)
        {
            
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "FileStorage", username);
            if (!Directory.Exists(directoryPath))
            {
                AddMemberStorage(username);
            }
            int i = 1;
            foreach (var file in files)
            {
                
                if (isSafeImageType(file.ContentType))
                {
                    string uploadTime = DateTime.Now.ToString() + "(" + i.ToString() + ")";
                    i++;
                    uploadTime = uploadTime.Replace("/", "-");
                    uploadTime = uploadTime.Replace(" ", "");
                    uploadTime = uploadTime.Replace(":", "-");
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    uploadTime += extension;
                    var filePath = Path.Combine(directoryPath, uploadTime);

                    await using (var outputStream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                    {
                        await file.CopyToAsync(outputStream);
                    }
                }
                else
                {
                    var filePath = Path.Combine(directoryPath, file.FileName);
                    await using (var outputStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                    {
                        await file.CopyToAsync(outputStream);
                    }
                }
                
            }
        }
        private static bool isSafeImageType(string? contentType)
        {
            return contentType is
                "image/jpeg" or
                "image/png" or
                "image/gif" or
                "image/webp" or
                "image/bmp" or
                "image/avif";
        }
    }
}

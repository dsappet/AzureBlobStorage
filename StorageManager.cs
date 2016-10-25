using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudFileStorageLibrary
{
    public class StorageManager
    {
        public CloudStorageAccount StorageAccount;
        private CloudBlobClient blobClient;
        private CloudFileClient fileClient;
        private CloudBlobContainer container;

        public StorageManager(string settingsString)
        {
            StorageAccount = CloudStorageAccount.Parse(settingsString);
            blobClient = StorageAccount.CreateCloudBlobClient();
            fileClient = StorageAccount.CreateCloudFileClient();
        }

        public async Task<bool> OpenContainerAsync(string containerName)
        {
            try
            {
                container = blobClient.GetContainerReference(containerName);
                await container.CreateIfNotExistsAsync();
                // To set this blob as public (don't need key) set this. 
                // container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                // Explicitly configure container for private access
                await
                    container.SetPermissionsAsync(new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Off
                    });
                //var permissions = container.GetPermissions();
                //permissions.PublicAccess = BlobContainerPublicAccessType.Off;
                //container.SetPermissions(permissions);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not open container for blob storage: " + ex.ToString());
            }
            finally
            {

            }
            return false;
        }

        // Returns false if overwrite is false and file exists
        public async Task<bool> UploadFileAsync(string destinationName, string sourceFileName,
            bool overwriteExisting = true)
        {
            if (!overwriteExisting)
            {
                var exists = BlobExistsOnCloud(destinationName);
                if (exists) return false;
            }
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(destinationName);

            using (var fileStream = File.OpenRead(sourceFileName))
            {
                await blockBlob.UploadFromStreamAsync(fileStream);
            }
            return true;
        }
        // Returns false if overwrite is false and file exists
        public async Task<bool> UploadFileAsync(string destinationName, Stream sourceFile,
            bool overwriteExisting = true)
        {
            if (!overwriteExisting)
            {
                var exists = BlobExistsOnCloud(destinationName);
                if (exists) return false;
            }
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(destinationName);
            await blockBlob.UploadFromStreamAsync(sourceFile);
            //using (var fileStream = File.OpenRead(sourceFileName))
            //{
            //    await blockBlob.UploadFromStreamAsync(fileStream);
            //}
            return true;
        }

        public bool BlobExistsOnCloud(string blobName)
        {
            var blob = container.GetBlockBlobReference(blobName);
            return blob.Exists();

        }

        public List<IListBlobItem> GetBlobList()
        {
            return container.ListBlobs(null, true).ToList(); // use true for flat listing, marshalling to block blobs

        }

        public string GetBlobPathWithSas(IListBlobItem blob)
        {
            return (GetBlobPathWithSas(blob.ToString()));
        }
        public string GetBlobPathWithSas(string blobName)
        {
            // Get container reference
            // THen use it like this
            // <img src="@GetBlobPathWithSas("myImage")" />

            // Get the blob, in my case an image
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            // Generate a Shared Access Signature that expires after 5 minute, with Read and List access 
            // (A shorter expiry might be feasible for small files, while larger files might need a 
            // longer access period)
            string sas = container.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(5),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List
            });
            return (blob.Uri.ToString() + sas).ToString();
        }

        // returns false if the file doesn't exist. 
        public async Task<bool> DownloadFileAsync(string blobName, string destinationFileName)
        {
            if (!BlobExistsOnCloud(blobName)) return false;
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            using (var fileStream = File.OpenWrite(destinationFileName))
            {
                await blockBlob.DownloadToStreamAsync(fileStream);
            }
            return true;
        }

        // Return false if the file doesn't exist? Or should it return true?
        public async Task<bool> DeleteBlobAsync(string blobName)
        {
            if (!BlobExistsOnCloud(blobName)) return false;
            // Retrieve reference to a blob named "myblob.txt".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            // Delete the blob.
            blockBlob.Delete();
            return true;


        }
    }
}

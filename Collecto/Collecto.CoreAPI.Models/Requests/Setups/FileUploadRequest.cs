using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Collecto.CoreAPI.Models.Requests.Setups
{
    public class FileUploadRequest
    {
        [Required, NotNull]
        public string FileName { get; set; }

        [Required, NotNull]
        public IFormFile FileData { get; set; }
    }

    public class QRCodeUploadRequest : FileUploadRequest
    {
        [Required, NotNull]
        public string EmailAddress { get; set; }
    }

    public class CameraImageUploadRequest : FileUploadRequest
    {
        [Required, NotNull]
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }

    public class ProductImageUploaderByIdRequest
    {
        public int ItemId { get; set; }
    }

    public class ProductImageUploaderBaseRequest : ProductImageUploaderByIdRequest
    {
        [Required, NotNull]
        public short ImageOf { get; set; }
        public int? SkuId { get; set; }
        public int? BrandId { get; set; }
        public int? ProductId { get; set; }
        public int? SubsystemId { get; set; }
    }

    public class ProductImageUploaderRequest : ProductImageUploaderBaseRequest
    {
        [Required, NotNull]
        public string FileName { get; set; }

        [Required, NotNull]
        public IFormFile FileImage { get; set; }
    }

    public class MultipleFilesUploadRequest
    {
        public int SubsystemId { get; set; }

        [Required, Range(minimum: 1, maximum: int.MaxValue, ConvertValueInInvariantCulture = true, ErrorMessage = "User Id is required.")]
        public int UserId { get; set; }
        public List<IFormFile> Attachments { get; set; }
    }
}

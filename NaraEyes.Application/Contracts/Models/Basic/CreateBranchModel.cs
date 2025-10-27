using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Application.Contracts.Models.Basic
{
    public class CreateBranchModel
    {
        [Required (ErrorMessage ="نام شعبه الزامی میباشد")]
        public string Name { get; set; } = null!;
        public string? ShortName { get;  set; }
        [Required(ErrorMessage = "کد شعبه الزامی میباشد")]
        public int Code { get;  set; }
        [Required(ErrorMessage = "سرپرستی شعبه الزامی میباشد")]
        public Guid SupervisionId { get;  set; }
        public string? Address { get;  set; }
        public string? PostalCode { get;  set; }
        public string? Phone { get;  set; }
        public decimal? Latitude { get;  set; }
        public decimal? Longitude { get;  set; }
        public bool IsActive { get;  set; } = true;
    }
    public class UpdateBranchModel: CreateBranchModel
    {
        public Guid Id { get; set; }
    }
}

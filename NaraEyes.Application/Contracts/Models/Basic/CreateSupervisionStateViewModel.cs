using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Application.Contracts.Models.Basic
{
    public class CreateSupervisionStateViewModel
    {
        [Required]
        public string Name { get; set; }
        [Range(1, int.MaxValue)]
        public int Code { get; set; }
        [Required]
        public string ShortName { get; set; }
    }
        
        
 
}

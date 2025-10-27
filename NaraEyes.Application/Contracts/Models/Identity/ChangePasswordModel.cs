namespace NaraEyes.Application.Contracts.Models.Identity
{
    public sealed class ChangePasswordModel
    {
        public ChangePasswordModel()
        {
            
        }
        public ChangePasswordModel(string currentPaaword, string newPassword, string confirmPassword)
        {
            CurrentPaaword = currentPaaword;
            NewPassword = newPassword;
            ConfirmPassword = confirmPassword;
        }
        public string CurrentPaaword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
        
        
        
       

}

namespace NaraEyes.Domain.Enumerations
{
    public enum HealthStatus {
        Unknown = -1, 
        Online = 0,   
        Offline = 1,  
        PowerOff = 2, 
        DeviceNotFound = 3, 
        HardwareError = 4,  
        UserError = 5, 
        Busy = 6,      
        FraudAttempt = 7,   
        PotentialFraud = 8
    }
}

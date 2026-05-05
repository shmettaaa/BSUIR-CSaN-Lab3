namespace CSaN_Lab3_Backend.Entities;

public class FileMetadata
{
    public int Id { get; set; }                     

    public string RelativePath { get; set; } = string.Empty;  

    public string FileName { get; set; } = string.Empty;      

    public long Size { get; set; }                          

    public string ContentType { get; set; } = string.Empty;   

    public DateTime CreatedAt { get; set; }                  

    public DateTime ModifiedAt { get; set; }                
}
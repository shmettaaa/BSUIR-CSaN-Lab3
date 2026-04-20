namespace CSaN_Lab3_Backend.Dtos;

public class FileTransferRequestDto
{
    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;
}
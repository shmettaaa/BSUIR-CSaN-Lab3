const BASE_URL = "http://localhost:5226/api/files";

export interface FileMetadata {
    relativePath: string;
    fileName: string;
    size: number;
    contentType: string;
    createdAt: string;
    modifiedAt: string;
}

export const downloadFile = async (path: string) => {
    const response = await fetch(`${BASE_URL}/content?path=${encodeURIComponent(path)}`);
    if (!response.ok) throw new Error("Ошибка скачивания файла");

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = path.split("/").pop() || "file";
    a.click();
    window.URL.revokeObjectURL(url);
};

export const openFile = (path: string) => {
    window.open(`${BASE_URL}/content?path=${encodeURIComponent(path)}&mode=open`, "_blank");
};

export const uploadFile = async (path: string, file: File) => {
    const response = await fetch(`${BASE_URL}?path=${encodeURIComponent(path)}`, {
        method: "PUT",
        body: file
    });
    if (!response.ok) throw new Error("Ошибка загрузки файла");
};

export const appendToFile = async (path: string, text: string) => {
    const response = await fetch(`${BASE_URL}?path=${encodeURIComponent(path)}`, {
        method: "POST",
        body: text
    });
    if (!response.ok) throw new Error("Ошибка при добавлении");
};

export const getFiles = async (): Promise<string[]> => {
    const response = await fetch(`${BASE_URL}`);
    if (!response.ok) throw new Error("Ошибка получения списка файлов");
    return await response.json();
};

export const deleteFile = async (path: string) => {
    const response = await fetch(`${BASE_URL}?path=${encodeURIComponent(path)}`, {
        method: "DELETE"
    });
    if (!response.ok) throw new Error("Ошибка удаления файла");
};

export const copyFile = async (sourcePath: string, destinationPath: string) => {
    const response = await fetch(`${BASE_URL}/copy`, {
        method: "COPY",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sourcePath, destinationPath })
    });
    if (!response.ok) throw new Error("Ошибка копирования файла");
};

export const moveFile = async (sourcePath: string, destinationPath: string) => {
    const response = await fetch(`${BASE_URL}/move`, {
        method: "MOVE",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sourcePath, destinationPath })
    });
    if (!response.ok) throw new Error("Ошибка перемещения файла");
};

export const getFileMetadata = async (path: string): Promise<FileMetadata> => {
    const response = await fetch(`${BASE_URL}/metadata?path=${encodeURIComponent(path)}`);
    if (!response.ok) throw new Error("Ошибка получения свойств файла");
    return await response.json();
};
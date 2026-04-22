const BASE_URL = "http://localhost:5226/api/files";

export const downloadFile = async (path: string) => {
    const response = await fetch(`${BASE_URL}/${path}`);

    if (!response.ok) {
        throw new Error("Ошибка скачивания файла");
    }

    const blob = await response.blob();

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = path.split("/").pop() || "file";
    a.click();
};

export const uploadFile = async (path: string, file: File) => {
    const response = await fetch(`${BASE_URL}/${path}`, {
        method: "PUT",
        body: file
    });

    if (!response.ok) {
        throw new Error("Ошибка загрузки файла");
    }
};

export const appendToFile = async (path: string, text: string) => {
    const response = await fetch(`${BASE_URL}/${path}`, {
        method: "POST",
        body: text
    });

    if (!response.ok) {
        throw new Error("Ошибка при добавлении");
    }
};

export const getFiles = async (): Promise<string[]> => {
    const response = await fetch(`${BASE_URL}`);

    if (!response.ok) {
        throw new Error("Ошибка получения списка файлов");
    }

    return await response.json();
};

export const deleteFile = async (path: string) => {
    const response = await fetch(`${BASE_URL}/${path}`, {
        method: "DELETE"
    });

    if (!response.ok) {
        throw new Error("Ошибка удаления файла");
    }
};

export const copyFile = async (sourcePath: string, destinationPath: string) => {
    const response = await fetch(`${BASE_URL}/copy`, {
        method: "COPY",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            sourcePath,
            destinationPath
        })
    });

    if (!response.ok) {
        throw new Error("Ошибка копирования файла");
    }
};

export const moveFile = async (sourcePath: string, destinationPath: string) => {
    const response = await fetch(`${BASE_URL}/move`, {
        method: "MOVE",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            sourcePath,
            destinationPath
        })
    });

    if (!response.ok) {
        throw new Error("Ошибка перемещения файла");
    }
};
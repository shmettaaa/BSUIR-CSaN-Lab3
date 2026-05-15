import { useEffect, useState } from "react";
import {
    downloadFile,
    uploadFile,
    appendToFile,
    getFiles,
    deleteFile,
    copyFile,
    moveFile,
    openFile,
    getFileMetadata,
    type FileMetadata
} from "../api/fileApi";

export const FileManager = () => {
    const [selectedFile, setSelectedFile] = useState<string | null>(null);
    const [file, setFile] = useState<File | null>(null);
    const [uploadPath, setUploadPath] = useState("");
    const [text, setText] = useState("");
    const [files, setFiles] = useState<string[]>([]);
    const [destinationPath, setDestinationPath] = useState("");

    const [showProperties, setShowProperties] = useState(false);
    const [metadata, setMetadata] = useState<FileMetadata | null>(null);

    useEffect(() => {
        const fetchFiles = async () => {
            try {
                const data = await getFiles();
                setFiles(data);
            } catch {
                alert("Не удалось загрузить список файлов");
            }
        };
        fetchFiles();
    }, []);

    const loadFiles = async () => {
        try {
            const data = await getFiles();
            setFiles(data);
        } catch {
            alert("Не удалось загрузить список файлов");
        }
    };

    const handleDownload = async () => {
        if (!selectedFile) return;
        await downloadFile(selectedFile);
    };

    const handleOpen = () => {
        if (!selectedFile) return;
        openFile(selectedFile);
    };

    const handleDelete = async () => {
        if (!selectedFile) return;
        if (!window.confirm("Удалить файл?")) return;
        await deleteFile(selectedFile);
        setSelectedFile(null);
        loadFiles();
    };

    const handleUpload = async () => {
        if (!file) return;
        await uploadFile(uploadPath || file.name, file);
        setFile(null);
        setUploadPath("");
        loadFiles();
    };

    const handleAppend = async () => {
        if (!selectedFile || !text.trim()) return;
        await appendToFile(selectedFile, text);
        setText("");
        loadFiles();
    };

    const handleCopy = async () => {
        if (!selectedFile || !destinationPath) return;
        await copyFile(selectedFile, destinationPath);
        setDestinationPath("");
        loadFiles();
    };

    const handleMove = async () => {
        if (!selectedFile || !destinationPath) return;
        await moveFile(selectedFile, destinationPath);
        setDestinationPath("");
        setSelectedFile(null);
        loadFiles();
    };

    const handleShowProperties = async () => {
        if (!selectedFile) return;
        try {
            const data = await getFileMetadata(selectedFile);
            setMetadata(data);
            setShowProperties(true);
        } catch {
            alert("Не удалось загрузить свойства файла");
        }
    };

    const formatSize = (bytes: number): string => {
        if (bytes < 1024) return `${bytes} Б`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)} КБ`;
        return `${(bytes / (1024 * 1024)).toFixed(2)} МБ`;
    };

    return (
        <div style={{
            width: "100%",
            minHeight: "100vh",
            padding: "40px",
            boxSizing: "border-box",
            fontFamily: "Arial",
            background: "#f5f6f8"
        }}>
            <h1 style={{ marginBottom: "30px" }}>Файловое хранилище</h1>

            <div style={{
                display: "grid",
                gridTemplateColumns: "1fr 400px",
                gap: "30px"
            }}>
                <div style={{
                    background: "#fff",
                    padding: "20px",
                    borderRadius: "12px",
                    border: "1px solid #ddd",
                    display: "flex",
                    flexDirection: "column"
                }}>
                    <div style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center"
                    }}>
                        <h3>Файлы</h3>
                        <button onClick={loadFiles}>Обновить</button>
                    </div>

                    <div style={{
                        height: "450px",
                        overflowY: "auto",
                        marginTop: "10px"
                    }}>
                        {files.map((f) => (
                            <div
                                key={f}
                                onClick={() => setSelectedFile(f)}
                                style={{
                                    padding: "10px",
                                    marginBottom: "8px",
                                    borderRadius: "6px",
                                    border: "1px solid #eee",
                                    cursor: "pointer",
                                    background: selectedFile === f ? "#e3f2fd" : "#fff"
                                }}
                            >
                                {f}
                            </div>
                        ))}
                    </div>

                    <div style={{ display: "flex", gap: "10px", marginTop: "15px" }}>
                        <button onClick={handleDownload} disabled={!selectedFile} style={{ flex: 1 }}>
                            Скачать
                        </button>
                        <button onClick={handleOpen} disabled={!selectedFile} style={{ flex: 1 }}>
                            Открыть
                        </button>
                        <button onClick={handleShowProperties} disabled={!selectedFile} style={{ flex: 1 }}>
                            Свойства
                        </button>
                        <button onClick={handleDelete} disabled={!selectedFile} style={{ flex: 1 }}>
                            Удалить
                        </button>
                    </div>
                </div>

                <div style={{ display: "flex", flexDirection: "column", gap: "20px" }}>

                    <div style={{
                        background: "#fff",
                        padding: "20px",
                        borderRadius: "12px",
                        border: "1px solid #ddd"
                    }}>
                        <h3>Загрузка файла</h3>
                        <input
                            type="file"
                            onChange={(e) => setFile(e.target.files?.[0] || null)}
                        />
                        <input
                            value={uploadPath}
                            onChange={(e) => setUploadPath(e.target.value)}
                            placeholder="Путь (необязательно)"
                            style={{ width: "100%", marginTop: "10px" }}
                        />
                        <button
                            onClick={handleUpload}
                            disabled={!file}
                            style={{ marginTop: "10px", width: "100%" }}
                        >
                            Загрузить
                        </button>
                    </div>

                    <div style={{
                        background: "#fff",
                        padding: "20px",
                        borderRadius: "12px",
                        border: "1px solid #ddd"
                    }}>
                        <h3>Добавить текст</h3>
                        <p style={{ fontSize: "14px", color: "#666" }}>
                            {selectedFile ? `Файл: ${selectedFile}` : "Выберите файл"}
                        </p>
                        <textarea
                            value={text}
                            onChange={(e) => setText(e.target.value)}
                            rows={5}
                            style={{ width: "100%" }}
                        />
                        <button
                            onClick={handleAppend}
                            disabled={!selectedFile || !text.trim()}
                            style={{ marginTop: "10px", width: "100%" }}
                        >
                            Добавить в файл
                        </button>
                    </div>

                    <div style={{
                        background: "#fff",
                        padding: "20px",
                        borderRadius: "12px",
                        border: "1px solid #ddd"
                    }}>
                        <h3>Копировать/Переместить</h3>
                        <p style={{ fontSize: "14px", color: "#666" }}>
                            {selectedFile ? `Файл: ${selectedFile}` : "Выберите файл"}
                        </p>
                        <input
                            value={destinationPath}
                            onChange={(e) => setDestinationPath(e.target.value)}
                            placeholder="Новый путь"
                            style={{ width: "100%", marginBottom: "10px" }}
                        />
                        <div style={{ display: "flex", gap: "10px" }}>
                            <button
                                onClick={handleCopy}
                                disabled={!selectedFile || !destinationPath}
                                style={{ flex: 1 }}
                            >
                                Копировать
                            </button>
                            <button
                                onClick={handleMove}
                                disabled={!selectedFile || !destinationPath}
                                style={{ flex: 1 }}
                            >
                                Переместить
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            {/* Модальное окно свойств файла */}
            {showProperties && metadata && (
                <div style={{
                    position: "fixed", top: 0, left: 0, width: "100%", height: "100%",
                    backgroundColor: "rgba(0,0,0,0.5)", display: "flex", justifyContent: "center", alignItems: "center",
                    zIndex: 1000
                }}>
                    <div style={{
                        background: "#fff", padding: "20px", borderRadius: "12px",
                        width: "450px", maxWidth: "90%"
                    }}>
                        <h3>Свойства файла</h3>
                        <table style={{ width: "100%", borderCollapse: "collapse", marginTop: "10px" }}>
                            <tbody>
                            <tr>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}><b>Имя:</b></td>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}>{metadata.fileName}</td>
                            </tr>
                            <tr>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}><b>Путь:</b></td>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}>{metadata.relativePath}</td>
                            </tr>
                            <tr>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}><b>Размер:</b></td>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}>{formatSize(metadata.size)}</td>
                            </tr>
                            <tr>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}><b>Тип:</b></td>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}>{metadata.contentType}</td>
                            </tr>
                            <tr>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}><b>Создан (UTC):</b></td>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}>{new Date(metadata.createdAt).toLocaleString()}</td>
                            </tr>
                            <tr>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}><b>Изменён (UTC):</b></td>
                                <td style={{ padding: "8px", borderBottom: "1px solid #eee" }}>{new Date(metadata.modifiedAt).toLocaleString()}</td>
                            </tr>
                            </tbody>
                        </table>
                        <div style={{ marginTop: "20px", textAlign: "right" }}>
                            <button onClick={() => setShowProperties(false)} style={{ padding: "6px 12px" }}>Закрыть</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};
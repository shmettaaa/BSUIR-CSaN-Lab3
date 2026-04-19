import { useEffect, useState } from "react";
import {
    downloadFile,
    uploadFile,
    appendToFile,
    getFiles,
} from "../api/fileApi";

export const FileManager = () => {
    const [path, setPath] = useState("");
    const [selectedFile, setSelectedFile] = useState<string | null>(null);
    const [file, setFile] = useState<File | null>(null);
    const [text, setText] = useState("");
    const [files, setFiles] = useState<string[]>([]);

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

    const handleUpload = async () => {
        if (!file) return;

        await uploadFile(path || file.name, file);
        setFile(null);
        loadFiles();
    };

    const handleAppend = async () => {
        if (!text.trim()) return;

        await appendToFile(path, text);
        setText("");
    };

    return (
        <div
            style={{
                width: "100%",
                minHeight: "100vh",
                padding: "40px",
                boxSizing: "border-box",
                fontFamily: "Arial, sans-serif",
                backgroundColor: "#fafafa",
            }}
        >
            <h1 style={{ marginBottom: "30px", fontWeight: 600 }}>
                File Storage System
            </h1>

            <div
                style={{
                    marginBottom: "30px",
                    padding: "20px",
                    background: "#fff",
                    border: "1px solid #e5e5e5",
                    borderRadius: "10px",
                }}
            >
                <h3 style={{ marginBottom: "10px" }}>Путь к файлу</h3>

                <input
                    value={path}
                    onChange={(e) => setPath(e.target.value)}
                    placeholder="example: folder/file.txt"
                    style={{
                        width: "100%",
                        padding: "12px",
                        border: "1px solid #ddd",
                        borderRadius: "8px",
                    }}
                />
            </div>

            <div
                style={{
                    display: "grid",
                    gridTemplateColumns: "1fr 360px",
                    gap: "30px",
                }}
            >
                <div
                    style={{
                        background: "#fff",
                        border: "1px solid #e5e5e5",
                        borderRadius: "10px",
                        padding: "20px",
                    }}
                >
                    <div
                        style={{
                            display: "flex",
                            justifyContent: "space-between",
                            marginBottom: "15px",
                        }}
                    >
                        <h3 style={{ margin: 0 }}>Файлы</h3>

                        <button
                            onClick={loadFiles}
                            style={{
                                padding: "8px 12px",
                                border: "1px solid #ccc",
                                borderRadius: "6px",
                                background: "#fff",
                                cursor: "pointer",
                            }}
                        >
                            Обновить
                        </button>
                    </div>

                    <div style={{ minHeight: "400px" }}>
                        {files.length === 0 ? (
                            <p style={{ color: "#999" }}>Нет файлов</p>
                        ) : (
                            files.map((f) => (
                                <div
                                    key={f}
                                    onClick={() => setSelectedFile(f)}
                                    style={{
                                        padding: "10px",
                                        marginBottom: "8px",
                                        borderRadius: "6px",
                                        border: "1px solid #eee",
                                        cursor: "pointer",
                                        background:
                                            selectedFile === f
                                                ? "#f0f6ff"
                                                : "#fff",
                                    }}
                                >
                                    {f}
                                </div>
                            ))
                        )}
                    </div>

                    <button
                        onClick={handleDownload}
                        disabled={!selectedFile}
                        style={{
                            width: "100%",
                            marginTop: "15px",
                            padding: "12px",
                            borderRadius: "8px",
                            border: "1px solid #ccc",
                            background: selectedFile ? "#fff" : "#f5f5f5",
                            cursor: selectedFile ? "pointer" : "not-allowed",
                        }}
                    >
                        Скачать файл
                    </button>
                </div>

                <div style={{ display: "flex", flexDirection: "column", gap: "20px" }}>

                    <div
                        style={{
                            background: "#fff",
                            border: "1px solid #e5e5e5",
                            borderRadius: "10px",
                            padding: "20px",
                        }}
                    >
                        <h3 style={{ marginBottom: "10px" }}>Загрузка файла</h3>

                        <input
                            type="file"
                            onChange={(e) =>
                                setFile(e.target.files ? e.target.files[0] : null)
                            }
                            style={{ marginBottom: "12px" }}
                        />

                        <button
                            onClick={handleUpload}
                            style={{
                                width: "100%",
                                padding: "10px",
                                borderRadius: "8px",
                                border: "1px solid #ccc",
                                background: "#fff",
                                cursor: "pointer",
                            }}
                        >
                            Загрузить
                        </button>
                    </div>

                    <div
                        style={{
                            background: "#fff",
                            border: "1px solid #e5e5e5",
                            borderRadius: "10px",
                            padding: "20px",
                        }}
                    >
                        <h3 style={{ marginBottom: "10px" }}>
                            Добавить текст
                        </h3>

                        <textarea
                            value={text}
                            onChange={(e) => setText(e.target.value)}
                            rows={6}
                            style={{
                                width: "100%",
                                padding: "10px",
                                borderRadius: "8px",
                                border: "1px solid #ddd",
                                marginBottom: "10px",
                            }}
                        />

                        <button
                            onClick={handleAppend}
                            style={{
                                width: "100%",
                                padding: "10px",
                                borderRadius: "8px",
                                border: "1px solid #ccc",
                                background: "#fff",
                                cursor: "pointer",
                            }}
                        >
                            Добавить
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};
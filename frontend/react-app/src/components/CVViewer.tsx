import React from "react";

interface CVViewerProps {
    fileUrl?: string;
    fileName?: string;
}

const CVViewer: React.FC<CVViewerProps> = ({ fileUrl, fileName }) => {
    if (!fileUrl) return <div className="p-4 border rounded text-gray-500 bg-gray-50">No CV document attached</div>;
    return (
        <div className="border rounded p-4">
            <h4 className="font-medium mb-2">Attached Document</h4>
            <div className="flex items-center justify-between">
                <span className="text-sm text-gray-600">{fileName || 'Document'}</span>
                <a href={fileUrl} target="_blank" rel="noreferrer" className="text-blue-500 hover:underline text-sm">Download</a>
            </div>
        </div>
    );
};
export default CVViewer;

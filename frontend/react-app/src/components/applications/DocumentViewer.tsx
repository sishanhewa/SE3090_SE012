import { useState } from 'react';
import {
  useApplicationDocuments,
  applicationDetailsApi,
  type DocumentItem,
} from '../../api/applicationDetailsApi';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { FileText, Upload, Download, Eye, Loader2, File } from 'lucide-react';

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

function getFileIcon(fileType: string) {
  switch (fileType) {
    case 'CV':
      return <FileText className="h-5 w-5 text-blue-500" />;
    case 'Certificate':
      return <File className="h-5 w-5 text-green-500" />;
    default:
      return <File className="h-5 w-5 text-gray-500" />;
  }
}

interface DocumentViewerProps {
  applicationId: string;
  canUpload?: boolean;
}

export default function DocumentViewer({ applicationId, canUpload = false }: DocumentViewerProps) {
  const { documents, loading, refresh } = useApplicationDocuments(applicationId);
  const [uploading, setUploading] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploading(true);
    try {
      await applicationDetailsApi.uploadDocument(applicationId, file);
      refresh();
    } catch (error) {
      console.error('Upload failed', error);
      alert('Failed to upload document. Please try again.');
    } finally {
      setUploading(false);
      e.target.value = '';
    }
  };

  const handlePreview = (doc: DocumentItem) => {
    if (doc.fileName.endsWith('.pdf')) {
      setPreviewUrl(doc.fileUrl);
    } else {
      // For non-PDF files, just download
      window.open(doc.fileUrl, '_blank');
    }
  };

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-lg">Documents & CV</CardTitle>
          {canUpload && (
            <div className="relative">
              <input
                type="file"
                id="doc-upload"
                className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
                accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
                onChange={handleUpload}
                disabled={uploading}
              />
              <Button variant="outline" size="sm" className="gap-2" disabled={uploading}>
                {uploading ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Upload className="h-4 w-4" />
                )}
                Upload
              </Button>
            </div>
          )}
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex items-center gap-2 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading documents...
            </div>
          ) : documents.length === 0 ? (
            <p className="text-muted-foreground">No documents uploaded yet.</p>
          ) : (
            <div className="space-y-3">
              {documents.map((doc: DocumentItem) => (
                <div
                  key={doc.id}
                  className="flex items-center gap-3 p-3 rounded-lg border bg-card hover:bg-muted/50 transition-colors"
                >
                  {getFileIcon(doc.fileType)}
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{doc.fileName}</p>
                    <p className="text-xs text-muted-foreground">
                      {formatFileSize(doc.fileSizeBytes)} · {new Date(doc.createdAt).toLocaleDateString()}
                    </p>
                  </div>
                  <Badge variant="secondary" className="text-xs">
                    {doc.fileType}
                  </Badge>
                  <div className="flex gap-1">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => handlePreview(doc)}
                      title="Preview"
                    >
                      <Eye className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => window.open(doc.fileUrl, '_blank')}
                      title="Download"
                    >
                      <Download className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* PDF Preview Modal */}
      {previewUrl && (
        <div
          className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-8"
          onClick={() => setPreviewUrl(null)}
        >
          <div
            className="bg-background rounded-lg shadow-xl w-full max-w-4xl h-[80vh] flex flex-col"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between p-4 border-b">
              <h3 className="font-semibold">Document Preview</h3>
              <Button variant="ghost" size="sm" onClick={() => setPreviewUrl(null)}>
                Close
              </Button>
            </div>
            <div className="flex-1 p-4">
              <iframe
                src={previewUrl}
                className="w-full h-full rounded border"
                title="Document Preview"
              />
            </div>
          </div>
        </div>
      )}
    </>
  );
}

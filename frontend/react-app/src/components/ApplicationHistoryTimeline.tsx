import React from "react";

interface HistoryEntry {
    id: string;
    fromStatus: string;
    toStatus: string;
    changedAt: string;
    changedBy: string;
}

interface Props {
    history: HistoryEntry[];
}

const ApplicationHistoryTimeline: React.FC<Props> = ({ history }) => {
    if (!history || history.length === 0) return <p className="text-sm text-gray-500">No history recorded.</p>;
    
    return (
        <div className="relative border-l border-gray-200 ml-3">
            {history.map((entry) => (
                <div key={entry.id} className="mb-6 ml-4">
                    <div className="absolute w-3 h-3 bg-blue-500 rounded-full mt-1.5 -left-1.5 border border-white"></div>
                    <time className="mb-1 text-xs font-normal leading-none text-gray-400">{new Date(entry.changedAt).toLocaleString()}</time>
                    <h3 className="text-sm font-semibold text-gray-900">Status changed to {entry.toStatus}</h3>
                    <p className="text-xs text-gray-500">By {entry.changedBy}</p>
                </div>
            ))}
        </div>
    );
};
export default ApplicationHistoryTimeline;

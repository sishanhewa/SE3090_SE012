import React, { useState, useRef, useEffect } from 'react';
import { MessageSquare, X, Send, Loader2, Bot, User } from 'lucide-react';
import { chatApi } from '../../api/chatApi';
import type { ChatMessage } from '../../types/chat';

export const ChatWidget: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([
    { role: 'assistant', content: 'Hello! I am your AI Recruitment Assistant. Ask me to find candidates, analyze applications, or schedule interviews.' }
  ]);
  const [inputValue, setInputValue] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const toggleChat = () => setIsOpen(!isOpen);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
    }
  }, [messages, isOpen]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputValue.trim() || isLoading) return;

    const userMessage: ChatMessage = { role: 'user', content: inputValue.trim() };
    const updatedMessages = [...messages, userMessage];

    setMessages(updatedMessages);
    setInputValue('');
    setIsLoading(true);

    try {
      const response = await chatApi.sendMessage({ messages: updatedMessages });
      setMessages([...updatedMessages, { role: 'assistant', content: response.response }]);
    } catch (error) {
      console.error('Failed to send message:', error);
      setMessages([
        ...updatedMessages,
        { role: 'assistant', content: 'Sorry, I encountered an error while processing your request. Please try again later.' }
      ]);
    } finally {
      setIsLoading(false);
    }
  };



  return (
    <>
      {/* Floating Action Button */}
      <button
        onClick={toggleChat}
        className={`fixed bottom-6 right-6 p-4 rounded-full shadow-lg text-white transition-all duration-300 z-50 ${
          isOpen ? 'bg-gray-800 scale-90' : 'bg-blue-600 hover:bg-blue-700 hover:scale-105'
        }`}
        aria-label="Toggle AI Chat"
      >
        {isOpen ? <X size={24} /> : <MessageSquare size={24} />}
      </button>

      {/* Chat Window */}
      {isOpen && (
        <div className="fixed bottom-24 right-6 w-96 h-[32rem] max-h-[80vh] bg-white rounded-2xl shadow-2xl flex flex-col overflow-hidden z-50 border border-gray-100 flex flex-col">
          {/* Header */}
          <div className="bg-blue-600 p-4 text-white flex items-center justify-between shadow-sm z-10">
            <div className="flex items-center space-x-2">
              <Bot size={24} className="text-blue-100" />
              <div>
                <h3 className="font-semibold text-lg leading-tight">AI Assistant</h3>
                <p className="text-xs text-blue-100">TalentFlow Coordinator</p>
              </div>
            </div>
            <button onClick={toggleChat} className="text-blue-100 hover:text-white transition-colors">
              <X size={20} />
            </button>
          </div>

          {/* Messages Area */}
          <div className="flex-1 p-4 overflow-y-auto bg-gray-50 flex flex-col space-y-4">
            {messages.map((msg, idx) => (
              <div
                key={idx}
                className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'} animate-in fade-in slide-in-from-bottom-2 duration-300`}
              >
                <div className={`flex items-start max-w-[85%] space-x-2 ${msg.role === 'user' ? 'flex-row-reverse space-x-reverse' : ''}`}>
                  <div className={`p-2 rounded-full flex-shrink-0 ${msg.role === 'user' ? 'bg-blue-100 text-blue-700' : 'bg-white shadow-sm text-gray-600 border border-gray-100'}`}>
                    {msg.role === 'user' ? <User size={16} /> : <Bot size={16} />}
                  </div>
                  <div
                    className={`p-3 rounded-2xl shadow-sm text-sm ${
                      msg.role === 'user'
                        ? 'bg-blue-600 text-white rounded-tr-sm'
                        : 'bg-white text-gray-800 border border-gray-100 rounded-tl-sm'
                    }`}
                    style={{ whiteSpace: 'pre-wrap' }}
                  >
                    {msg.content}
                  </div>
                </div>
              </div>
            ))}
            {isLoading && (
              <div className="flex justify-start">
                <div className="flex items-start space-x-2">
                  <div className="p-2 rounded-full bg-white shadow-sm text-gray-400 border border-gray-100">
                    <Bot size={16} />
                  </div>
                  <div className="p-3 bg-white text-gray-500 rounded-2xl rounded-tl-sm shadow-sm border border-gray-100 flex items-center space-x-2">
                    <Loader2 size={16} className="animate-spin" />
                    <span className="text-xs font-medium">Thinking...</span>
                  </div>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Input Area */}
          <div className="p-4 bg-white border-t border-gray-100">
            <form onSubmit={handleSendMessage} className="flex items-center space-x-2">
              <input
                type="text"
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                placeholder="Ask about candidates, jobs, or interviews..."
                className="flex-1 border border-gray-200 rounded-full px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
                disabled={isLoading}
              />
              <button
                type="submit"
                disabled={!inputValue.trim() || isLoading}
                className="p-2 bg-blue-600 text-white rounded-full hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                <Send size={18} />
              </button>
            </form>
          </div>
        </div>
      )}
    </>
  );
};

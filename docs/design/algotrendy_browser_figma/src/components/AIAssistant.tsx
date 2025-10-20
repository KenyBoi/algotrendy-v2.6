import { useState, useRef, useEffect } from 'react';
import { Card } from './ui/card';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { ScrollArea } from './ui/scroll-area';
import { Badge } from './ui/badge';
import { Bot, X, Send, Minimize2, Maximize2, Sparkles } from 'lucide-react';

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

interface AIAssistantProps {
  isOpen: boolean;
  onClose: () => void;
  isMinimized: boolean;
  onToggleMinimize: () => void;
}

export function AIAssistant({ isOpen, onClose, isMinimized, onToggleMinimize }: AIAssistantProps) {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      role: 'assistant',
      content: 'Hey! 👋 I\'m MEM, your AI buddy for exploring markets and building strategies. I\'ve got access to some pretty cool datasets and can help you test wild ideas. What are you thinking about today?',
      timestamp: new Date()
    }
  ]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages]);

  const handleSend = async () => {
    if (!input.trim()) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      role: 'user',
      content: input,
      timestamp: new Date()
    };

    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    // Call your .NET backend AI service
    try {
      const { aiService } = await import('../services');
      const response = await aiService.chat({
        message: input,
        history: messages.map(m => ({
          id: m.id,
          role: m.role,
          content: m.content,
          timestamp: m.timestamp
        })),
        context: {
          currentView: window.location.pathname
        }
      });

      if (response.success && response.data) {
        const assistantMessage: Message = {
          id: (Date.now() + 1).toString(),
          role: 'assistant',
          content: response.data.message,
          timestamp: new Date()
        };
        setMessages(prev => [...prev, assistantMessage]);
      } else {
        throw new Error('AI service returned unsuccessful response');
      }
    } catch (error) {
      console.error('AI API error:', error);
      const errorMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: 'Sorry, I\'m having trouble connecting to the AI service right now. Please ensure the backend API is running and try again.',
        timestamp: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  if (!isOpen) return null;

  return (
    <Card className={`fixed right-6 shadow-2xl z-50 flex flex-col transition-all duration-200 bg-slate-900 border-slate-700 ${
      isMinimized 
        ? 'bottom-6 w-80 h-16' 
        : 'bottom-6 w-96 h-[600px]'
    }`}>
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-slate-700 bg-slate-800">
        <div className="flex items-center gap-3">
          <div className="relative">
            <div className="w-8 h-8 rounded-lg bg-blue-600 flex items-center justify-center">
              <Bot className="h-4 w-4 text-white" />
            </div>
            <div className="absolute -top-0.5 -right-0.5 w-2 h-2 bg-green-500 rounded-full border border-slate-800" />
          </div>
          <div>
            <div className="text-sm font-medium text-gray-100 font-numeric">
              MEM
            </div>
            {!isMinimized && (
              <div className="text-xs text-gray-400">AI Assistant</div>
            )}
          </div>
        </div>
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="icon"
            onClick={onToggleMinimize}
            className="h-8 w-8 text-gray-400 hover:text-gray-300 hover:bg-slate-700"
          >
            {isMinimized ? <Maximize2 className="h-4 w-4" /> : <Minimize2 className="h-4 w-4" />}
          </Button>
          <Button
            variant="ghost"
            size="icon"
            onClick={onClose}
            className="h-8 w-8 text-gray-400 hover:text-gray-300 hover:bg-slate-700"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {!isMinimized && (
        <>
          {/* Messages */}
          <ScrollArea className="flex-1 p-4" ref={scrollRef}>
            <div className="space-y-4">
              {messages.map((message) => (
                <div
                  key={message.id}
                  className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}
                >
                  <div
                    className={`max-w-[80%] rounded-lg p-3 ${
                      message.role === 'user'
                        ? 'bg-blue-600 text-white'
                        : 'bg-slate-800 text-gray-100 border border-slate-700'
                    }`}
                  >
                    <div className="text-sm whitespace-pre-wrap">{message.content}</div>
                    <div
                      className={`text-xs mt-1 ${
                        message.role === 'user' ? 'text-blue-200' : 'text-gray-500'
                      }`}
                    >
                      {message.timestamp.toLocaleTimeString([], {
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </div>
                  </div>
                </div>
              ))}
              {isLoading && (
                <div className="flex justify-start">
                  <div className="bg-slate-800 border border-slate-700 rounded-lg p-3">
                    <div className="flex gap-1">
                      <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce"></div>
                      <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0.1s' }}></div>
                      <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }}></div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </ScrollArea>

          {/* Suggestions */}
          <div className="px-4 py-2 border-t border-slate-700 bg-slate-800/50">
            <div className="flex gap-2 flex-wrap">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setInput('What patterns are you seeing today?')}
                className="text-xs border-slate-600 text-gray-300 hover:bg-slate-700 hover:text-gray-200"
              >
                Show patterns
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setInput('Help me optimize this strategy')}
                className="text-xs border-slate-600 text-gray-300 hover:bg-slate-700 hover:text-gray-200"
              >
                Optimize together
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setInput('Any interesting opportunities?')}
                className="text-xs border-slate-600 text-gray-300 hover:bg-slate-700 hover:text-gray-200"
              >
                Find opportunities
              </Button>
            </div>
          </div>

          {/* Input */}
          <div className="p-4 border-t border-slate-700">
            <div className="flex gap-2">
              <Input
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder="Ask MEM anything..."
                disabled={isLoading}
                className="flex-1 bg-slate-800 border-slate-600 text-gray-100 placeholder:text-gray-500 
                           focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
              />
              <Button
                onClick={handleSend}
                disabled={isLoading || !input.trim()}
                size="icon"
                className="bg-blue-600 hover:bg-blue-700 text-white"
              >
                <Send className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </>
      )}
    </Card>
  );
}

export function AIAssistantButton({ onClick }: { onClick: () => void }) {
  return (
    <Button
      onClick={onClick}
      className="fixed bottom-6 right-6 h-14 w-14 rounded-xl shadow-lg bg-blue-600 hover:bg-blue-700 
                 border border-blue-500/50 z-40 transition-all hover:shadow-blue-500/20"
      size="icon"
    >
      <div className="relative">
        <Bot className="h-6 w-6 text-white" />
        <span className="absolute -top-1 -right-1 h-2.5 w-2.5 bg-green-500 rounded-full border border-slate-900"></span>
      </div>
    </Button>
  );
}

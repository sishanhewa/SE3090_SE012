import { useState, useEffect, useCallback } from 'react';
import {
  onboardingApi,
  type OnboardingTemplate,
  type EmployeeOnboardingTask,
  type CreateOnboardingTemplateRequest,
} from '../../api/onboardingApi';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Plus,
  CheckCircle2,
  Circle,
  ClipboardList,
  Loader2,
  GripVertical,
  Trash2,
} from 'lucide-react';

// ── Template List ──
interface OnboardingTemplateListProps {
  companyId: string;
  onChanged?: () => void;
}

export function OnboardingTemplateList({ companyId, onChanged }: OnboardingTemplateListProps) {
  const [templates, setTemplates] = useState<OnboardingTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);

  const fetchTemplates = useCallback(async () => {
    setLoading(true);
    try {
      const data = await onboardingApi.getTemplates(companyId);
      setTemplates(data);
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, [companyId]);

  useEffect(() => {
    fetchTemplates();
  }, [fetchTemplates]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold tracking-tight">Onboarding Templates</h2>
        <Button onClick={() => setShowCreate(!showCreate)} className="gap-2">
          <Plus className="h-4 w-4" />
          New Template
        </Button>
      </div>

      {showCreate && (
        <CreateTemplateForm
          companyId={companyId}
          onCreated={() => {
            setShowCreate(false);
            fetchTemplates();
            onChanged?.();
          }}
          onCancel={() => setShowCreate(false)}
        />
      )}

      {loading ? (
        <div className="flex items-center gap-2 text-muted-foreground py-8">
          <Loader2 className="h-4 w-4 animate-spin" /> Loading templates...
        </div>
      ) : templates.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            <ClipboardList className="h-10 w-10 mx-auto mb-4 opacity-50" />
            <p>No onboarding templates yet. Create one to get started.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4">
          {templates.map((template) => (
            <Card key={template.id}>
              <CardHeader>
                <CardTitle className="text-lg flex items-center justify-between">
                  <span>{template.name}</span>
                  <Badge variant="secondary">{template.tasks.length} tasks</Badge>
                </CardTitle>
                {template.description && (
                  <p className="text-sm text-muted-foreground">{template.description}</p>
                )}
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {template.tasks
                    .sort((a, b) => a.sortOrder - b.sortOrder)
                    .map((task, index) => (
                      <div
                        key={task.id}
                        className="flex items-center gap-3 text-sm p-2 rounded-md border bg-card"
                      >
                        <span className="text-muted-foreground font-mono text-xs w-6 text-right">
                          {index + 1}.
                        </span>
                        <span className="flex-1">{task.title}</span>
                        {task.isMandatory && (
                          <Badge variant="destructive" className="text-[10px]">
                            Required
                          </Badge>
                        )}
                      </div>
                    ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

// ── Create Template Form ──
interface CreateTemplateFormProps {
  companyId: string;
  onCreated: () => void;
  onCancel: () => void;
}

function CreateTemplateForm({ companyId, onCreated, onCancel }: CreateTemplateFormProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [tasks, setTasks] = useState<
    { title: string; description: string; sortOrder: number; isMandatory: boolean }[]
  >([{ title: '', description: '', sortOrder: 1, isMandatory: true }]);
  const [submitting, setSubmitting] = useState(false);

  const addTask = () => {
    setTasks([
      ...tasks,
      { title: '', description: '', sortOrder: tasks.length + 1, isMandatory: true },
    ]);
  };

  const removeTask = (index: number) => {
    setTasks(tasks.filter((_, i) => i !== index));
  };

  const updateTask = (index: number, field: string, value: string | boolean) => {
    const updated = [...tasks];
    (updated[index] as any)[field] = value;
    setTasks(updated);
  };

  const handleSubmit = async () => {
    if (!name.trim()) return;
    if (tasks.some((t) => !t.title.trim())) return;

    setSubmitting(true);
    try {
      const request: CreateOnboardingTemplateRequest = {
        name: name.trim(),
        description: description.trim() || undefined,
        tasks: tasks.map((t, i) => ({
          title: t.title.trim(),
          description: t.description.trim() || undefined,
          sortOrder: i + 1,
          isMandatory: t.isMandatory,
        })),
      };
      await onboardingApi.createTemplate(companyId, request);
      onCreated();
    } catch (error) {
      console.error('Failed to create template', error);
      alert('Failed to create onboarding template.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Create Onboarding Template</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="text-sm font-medium">Template Name *</label>
            <Input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g., Engineering Onboarding"
            />
          </div>
          <div>
            <label className="text-sm font-medium">Description</label>
            <Input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Optional description"
            />
          </div>
        </div>

        <div>
          <label className="text-sm font-medium mb-2 block">Tasks</label>
          <div className="space-y-2">
            {tasks.map((task, index) => (
              <div
                key={index}
                className="flex items-center gap-2 p-3 border rounded-lg bg-card"
              >
                <GripVertical className="h-4 w-4 text-muted-foreground" />
                <Input
                  className="flex-1"
                  value={task.title}
                  onChange={(e) => updateTask(index, 'title', e.target.value)}
                  placeholder={`Task ${index + 1} title`}
                />
                <label className="flex items-center gap-1 text-xs whitespace-nowrap">
                  <input
                    type="checkbox"
                    checked={task.isMandatory}
                    onChange={(e) => updateTask(index, 'isMandatory', e.target.checked)}
                  />
                  Required
                </label>
                {tasks.length > 1 && (
                  <Button variant="ghost" size="icon" onClick={() => removeTask(index)}>
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                )}
              </div>
            ))}
          </div>
          <Button variant="outline" size="sm" onClick={addTask} className="mt-2 gap-1">
            <Plus className="h-3 w-3" /> Add Task
          </Button>
        </div>

        <div className="flex gap-2 pt-2">
          <Button onClick={handleSubmit} disabled={submitting}>
            {submitting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
            Create Template
          </Button>
          <Button variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

// ── Employee Onboarding Progress ──
interface EmployeeOnboardingProgressProps {
  employeeId: string;
  refreshKey?: number;
}

export function EmployeeOnboardingProgress({ employeeId, refreshKey }: EmployeeOnboardingProgressProps) {
  const [tasks, setTasks] = useState<EmployeeOnboardingTask[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchTasks = useCallback(async () => {
    setLoading(true);
    try {
      const data = await onboardingApi.getEmployeeTasks(employeeId);
      setTasks(data);
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, [employeeId]);

  useEffect(() => {
    fetchTasks();
  }, [fetchTasks, refreshKey]);

  const handleComplete = async (taskId: string) => {
    try {
      await onboardingApi.completeTask(taskId);
      fetchTasks();
    } catch {
      alert('Failed to complete task.');
    }
  };

  const completed = tasks.filter((t) => t.isCompleted).length;
  const total = tasks.length;
  const percentage = total > 0 ? Math.round((completed / total) * 100) : 0;

  if (loading) {
    return (
      <Card>
        <CardContent className="py-6 flex items-center gap-2 text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" /> Loading onboarding tasks...
        </CardContent>
      </Card>
    );
  }

  if (tasks.length === 0) return null;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center justify-between">
          <span>Onboarding Progress</span>
          <Badge variant={percentage === 100 ? 'default' : 'secondary'}>
            {completed}/{total} ({percentage}%)
          </Badge>
        </CardTitle>
        {/* Progress bar */}
        <div className="w-full bg-muted rounded-full h-2 mt-2">
          <div
            className="bg-primary h-2 rounded-full transition-all duration-500"
            style={{ width: `${percentage}%` }}
          />
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {tasks.map((task) => (
            <div
              key={task.id}
              className={`flex items-center gap-3 p-3 rounded-lg border transition-colors ${
                task.isCompleted ? 'bg-muted/50' : 'bg-card hover:bg-muted/30'
              }`}
            >
              {task.isCompleted ? (
                <CheckCircle2 className="h-5 w-5 text-green-500 flex-shrink-0" />
              ) : (
                <button onClick={() => handleComplete(task.id)} title="Mark as completed">
                  <Circle className="h-5 w-5 text-muted-foreground hover:text-primary flex-shrink-0 cursor-pointer" />
                </button>
              )}
              <div className="flex-1 min-w-0">
                <p
                  className={`text-sm font-medium ${
                    task.isCompleted ? 'line-through text-muted-foreground' : ''
                  }`}
                >
                  {task.taskTitle}
                </p>
                {task.taskDescription && (
                  <p className="text-xs text-muted-foreground">{task.taskDescription}</p>
                )}
              </div>
              {task.isMandatory && !task.isCompleted && (
                <Badge variant="destructive" className="text-[10px]">
                  Required
                </Badge>
              )}
              {task.completedAt && (
                <span className="text-xs text-muted-foreground">
                  {new Date(task.completedAt).toLocaleDateString()}
                </span>
              )}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

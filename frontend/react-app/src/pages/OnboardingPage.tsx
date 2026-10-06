import { useState, useEffect } from 'react';
import { employeesApi, type EmployeeResponse } from '../api/employeesApi';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { CheckSquare, Briefcase, CalendarDays, CheckCircle } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { OnboardingTemplateList, EmployeeOnboardingProgress } from '../components/onboarding/OnboardingManagement';
import { useAuthStore } from '../store/authStore';
import { onboardingApi, type OnboardingTemplate } from '../api/onboardingApi';
import apiClient from '../api/apiClient';

export default function OnboardingPage() {
  const storedCompanyId = useAuthStore((state) => state.user?.companyId);
  const accessToken = useAuthStore((state) => state.accessToken);
  const [resolvedCompanyId, setResolvedCompanyId] = useState<string | undefined>(storedCompanyId);
  let tokenCompanyId: string | undefined;
  try { tokenCompanyId = accessToken ? JSON.parse(atob(accessToken.split('.')[1])).CompanyId : undefined; }
  catch { tokenCompanyId = undefined; }
  const companyId = storedCompanyId ?? tokenCompanyId ?? resolvedCompanyId;
  const [employees, setEmployees] = useState<EmployeeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeResponse | null>(null);
  const [templates, setTemplates] = useState<OnboardingTemplate[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState('');
  const [taskRefresh, setTaskRefresh] = useState(0);

  useEffect(() => {
    if (storedCompanyId || tokenCompanyId) { setResolvedCompanyId(storedCompanyId ?? tokenCompanyId); return; }
    apiClient.get<{ companyId?: string }>('/Auth/me')
      .then((response) => setResolvedCompanyId(response.data.companyId))
      .catch(() => setResolvedCompanyId(undefined));
  }, [storedCompanyId, tokenCompanyId]);

  const fetchEmployees = async () => {
    if (!companyId) { setLoading(false); return; }
    try {
      const data = await employeesApi.getAll(companyId);
      const availableTemplates = await onboardingApi.getTemplates(companyId);
      setTemplates(availableTemplates);
      // Filter only employees in Onboarding status
      setEmployees(data.items.filter((e: EmployeeResponse) => e.status === 'Onboarding'));
    } catch (error) {
      console.error('Failed to fetch employees', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchEmployees();
  }, [companyId]);

  const handleCompleteOnboarding = async () => {
    if (!selectedEmployee || !companyId) return;

    try {
      await employeesApi.update(selectedEmployee.id, companyId, {
        status: 'Active'
      });
      setSelectedEmployee(null);
      fetchEmployees();
    } catch (error) {
      console.error('Failed to complete onboarding', error);
    }
  };

  const handleAssignTemplate = async () => {
    if (!selectedEmployee || !selectedTemplateId) return;
    try {
      await onboardingApi.assignTemplate(selectedEmployee.id, selectedTemplateId);
      setTaskRefresh((value) => value + 1);
    } catch {
      alert('Could not assign this template. Tasks may already be assigned.');
    }
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Onboarding</h2>
          <p className="text-muted-foreground">Manage tasks for new hires.</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CheckSquare className="h-5 w-5 text-primary" />
            Employees in Onboarding
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex justify-center py-8 text-muted-foreground">Loading onboarding queue...</div>
          ) : employees.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground border-2 border-dashed rounded-lg">
              <CheckSquare className="h-12 w-12 mb-4 opacity-50" />
              <p>No employees are currently in onboarding.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Employee Name</TableHead>
                  <TableHead>Employee Number</TableHead>
                  <TableHead>Position</TableHead>
                  <TableHead>Start Date</TableHead>
                  <TableHead className="text-right">Action</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {employees.map((emp) => (
                  <TableRow key={emp.id}>
                    <TableCell className="font-medium">{emp.name}</TableCell>
                    <TableCell>{emp.employeeNumber}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Briefcase className="h-4 w-4 text-muted-foreground" />
                        {emp.position}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <CalendarDays className="h-4 w-4 text-muted-foreground" />
                        {new Date(emp.startDate).toLocaleDateString()}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="outline" size="sm" onClick={() => {
                        setSelectedEmployee(emp);
                      }}>
                        Manage Onboarding
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={!!selectedEmployee} onOpenChange={(open) => !open && setSelectedEmployee(null)}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Onboarding: {selectedEmployee?.name}</DialogTitle>
          </DialogHeader>
          <div className="space-y-6 mt-4">
            <div className="bg-muted p-4 rounded-md text-sm space-y-1">
              <div><strong>Position:</strong> {selectedEmployee?.position}</div>
              <div><strong>Start Date:</strong> {selectedEmployee ? new Date(selectedEmployee.startDate).toLocaleDateString() : ''}</div>
            </div>

            {selectedEmployee && (
              <EmployeeOnboardingProgress employeeId={selectedEmployee.id} refreshKey={taskRefresh} />
            )}

            {templates.length > 0 && (
              <div className="flex gap-2">
                <select className="flex-1 rounded-md border bg-background px-3 py-2" value={selectedTemplateId}
                  onChange={(event) => setSelectedTemplateId(event.target.value)}>
                  <option value="">Select onboarding template</option>
                  {templates.map((template) => <option value={template.id} key={template.id}>{template.name}</option>)}
                </select>
                <Button variant="outline" onClick={handleAssignTemplate} disabled={!selectedTemplateId}>Assign tasks</Button>
              </div>
            )}

            <Button 
              className="w-full gap-2" 
              onClick={handleCompleteOnboarding}
            >
              <CheckCircle className="h-4 w-4" /> Mark Onboarding Complete
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {companyId ? <div className="pt-8 border-t"><OnboardingTemplateList companyId={companyId} onChanged={fetchEmployees} /></div>
        : <p className="text-sm text-amber-700">Sign in with a company account to manage onboarding.</p>}
    </div>
  );
}

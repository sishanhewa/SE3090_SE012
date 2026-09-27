import { useState, useEffect } from 'react';
import { employeesApi, type EmployeeResponse } from '../api/employeesApi';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { CheckSquare, Briefcase, CalendarDays, CheckCircle } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';

export default function OnboardingPage() {
  const [employees, setEmployees] = useState<EmployeeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeResponse | null>(null);
  const [checklist, setChecklist] = useState({
    documents: false,
    equipment: false,
    accounts: false,
    orientation: false
  });

  const fetchEmployees = async () => {
    try {
      const data = await employeesApi.getAll('company-1');
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
  }, []);

  const handleCompleteOnboarding = async () => {
    if (!selectedEmployee) return;
    
    // Ensure all checklist items are completed
    if (!Object.values(checklist).every(Boolean)) {
      alert("Please complete all onboarding checklist items first.");
      return;
    }

    try {
      await employeesApi.update(selectedEmployee.id, 'company-1', {
        status: 'Active'
      });
      setSelectedEmployee(null);
      setChecklist({ documents: false, equipment: false, accounts: false, orientation: false });
      fetchEmployees();
    } catch (error) {
      console.error('Failed to complete onboarding', error);
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
                        setChecklist({ documents: false, equipment: false, accounts: false, orientation: false });
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
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Onboarding: {selectedEmployee?.name}</DialogTitle>
          </DialogHeader>
          <div className="space-y-6 mt-4">
            <div className="bg-muted p-4 rounded-md text-sm space-y-1">
              <div><strong>Position:</strong> {selectedEmployee?.position}</div>
              <div><strong>Start Date:</strong> {selectedEmployee ? new Date(selectedEmployee.startDate).toLocaleDateString() : ''}</div>
            </div>

            <div className="space-y-4">
              <h4 className="font-semibold">Checklist</h4>
              
              <label className="flex items-center gap-3 p-3 border rounded-md cursor-pointer hover:bg-accent/50 transition-colors">
                <input 
                  type="checkbox" 
                  className="w-5 h-5"
                  checked={checklist.documents} 
                  onChange={(e) => setChecklist({...checklist, documents: e.target.checked})} 
                />
                <span>Signed all tax and legal documents</span>
              </label>

              <label className="flex items-center gap-3 p-3 border rounded-md cursor-pointer hover:bg-accent/50 transition-colors">
                <input 
                  type="checkbox" 
                  className="w-5 h-5"
                  checked={checklist.equipment} 
                  onChange={(e) => setChecklist({...checklist, equipment: e.target.checked})} 
                />
                <span>Provided necessary equipment (Laptop, etc.)</span>
              </label>

              <label className="flex items-center gap-3 p-3 border rounded-md cursor-pointer hover:bg-accent/50 transition-colors">
                <input 
                  type="checkbox" 
                  className="w-5 h-5"
                  checked={checklist.accounts} 
                  onChange={(e) => setChecklist({...checklist, accounts: e.target.checked})} 
                />
                <span>Created IT and email accounts</span>
              </label>

              <label className="flex items-center gap-3 p-3 border rounded-md cursor-pointer hover:bg-accent/50 transition-colors">
                <input 
                  type="checkbox" 
                  className="w-5 h-5"
                  checked={checklist.orientation} 
                  onChange={(e) => setChecklist({...checklist, orientation: e.target.checked})} 
                />
                <span>Completed HR orientation</span>
              </label>
            </div>

            <Button 
              className="w-full gap-2" 
              onClick={handleCompleteOnboarding}
              disabled={!Object.values(checklist).every(Boolean)}
            >
              <CheckCircle className="h-4 w-4" /> Complete Onboarding
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

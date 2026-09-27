import { useState, useEffect } from 'react';
import { employeesApi, type EmployeeResponse, type CreateEmployeeRequest } from '../api/employeesApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Users, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';

export default function EmployeesPage() {
  const [employees, setEmployees] = useState<EmployeeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  
  const [formData, setFormData] = useState<CreateEmployeeRequest>({
    userId: '',
    departmentId: '00000000-0000-0000-0000-000000000000', // Default / fallback
    employeeNumber: '',
    position: '',
    startDate: '',
  });

  const fetchData = async () => {
    try {
      const data = await employeesApi.getAll('company-1');
      setEmployees(data.items);
    } catch (error) {
      console.error('Failed to fetch employees', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await employeesApi.create('company-1', {
        ...formData,
        startDate: new Date(formData.startDate).toISOString()
      });
      setIsModalOpen(false);
      setFormData({ 
        userId: '', departmentId: '00000000-0000-0000-0000-000000000000', employeeNumber: '',
        position: '', startDate: '' 
      });
      fetchData();
    } catch (error) {
      console.error('Failed to add employee', error);
    }
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Employees</h2>
          <p className="text-muted-foreground">Manage your organization's workforce.</p>
        </div>
        
        <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
          <DialogTrigger asChild>
            <Button className="gap-2">
              <Plus className="h-4 w-4" />
              Add Employee
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Onboard New Employee</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-4">
              <Input
                placeholder="User ID (Guid)"
                value={formData.userId}
                onChange={(e) => setFormData({ ...formData, userId: e.target.value })}
                required
              />
              <Input
                placeholder="Employee Number (e.g. EMP-001)"
                value={formData.employeeNumber}
                onChange={(e) => setFormData({ ...formData, employeeNumber: e.target.value })}
                required
              />
              <Input
                placeholder="Department ID (Guid)"
                value={formData.departmentId}
                onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                required
              />
              <Input
                placeholder="Position (Job Title)"
                value={formData.position}
                onChange={(e) => setFormData({ ...formData, position: e.target.value })}
                required
              />
              <Input
                type="date"
                placeholder="Start Date"
                value={formData.startDate}
                onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                required
              />
              <Button type="submit" className="w-full">Add Employee</Button>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5 text-primary" />
            Employee Directory
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex justify-center py-8 text-muted-foreground">Loading employees...</div>
          ) : employees.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground border-2 border-dashed rounded-lg">
              <Users className="h-12 w-12 mb-4 opacity-50" />
              <p>No employees onboarded yet.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Employee Number</TableHead>
                  <TableHead>Position</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {employees.map((employee) => (
                  <TableRow key={employee.id}>
                    <TableCell className="font-medium">{employee.name || employee.userId}</TableCell>
                    <TableCell>{employee.employeeNumber}</TableCell>
                    <TableCell>{employee.position}</TableCell>
                    <TableCell>
                      <Badge variant={employee.status === 'Active' ? 'default' : employee.status === 'Onboarding' ? 'secondary' : 'outline'}>
                        {employee.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <Link to={`/employees/${employee.id}`}>
                        <Button variant="outline" size="sm">View Profile</Button>
                      </Link>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

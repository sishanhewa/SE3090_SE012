import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { employeesApi, type EmployeeResponse } from '../api/employeesApi';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ArrowLeft, User, Briefcase, Calendar, Building2, UserMinus } from 'lucide-react';

export default function EmployeeDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [employee, setEmployee] = useState<EmployeeResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchDetails = async () => {
    if (!id) return;
    try {
      const data = await employeesApi.getById(id, 'company-1');
      setEmployee(data);
    } catch (error) {
      console.error('Failed to fetch employee details', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDetails();
  }, [id]);

  const handleTerminate = async () => {
    if (!id || !window.confirm('Are you sure you want to terminate this employee?')) return;
    try {
      await employeesApi.update(id, 'company-1', { status: 'Terminated' });
      fetchDetails();
    } catch (error) {
      console.error('Failed to terminate employee', error);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading employee details...</div>;
  }

  if (!employee) {
    return <div className="p-8 text-center text-destructive">Employee not found.</div>;
  }

  return (
    <div className="p-8 space-y-6 max-w-4xl mx-auto">
      <Link to="/employees">
        <Button variant="ghost" className="mb-4 gap-2">
          <ArrowLeft className="h-4 w-4" />
          Back to Employees
        </Button>
      </Link>
      
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-card p-6 rounded-lg border shadow-sm">
        <div className="flex items-center gap-4">
          <div className="h-16 w-16 bg-primary/10 rounded-full flex items-center justify-center">
            <User className="h-8 w-8 text-primary" />
          </div>
          <div>
            <h2 className="text-3xl font-bold tracking-tight">{employee.name}</h2>
            <div className="flex items-center gap-3 mt-2">
              <Badge variant={employee.status === 'Active' ? 'default' : employee.status === 'Onboarding' ? 'secondary' : 'destructive'}>
                {employee.status}
              </Badge>
              <span className="text-muted-foreground text-sm flex items-center gap-1">
                <Briefcase className="h-4 w-4" /> {employee.position}
              </span>
            </div>
          </div>
        </div>
        
        {employee.status === 'Active' && (
          <Button variant="destructive" onClick={handleTerminate} className="gap-2">
            <UserMinus className="h-4 w-4" /> Terminate Employment
          </Button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Employment Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center gap-4">
              <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                <Building2 className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-sm font-medium">Employee Number</p>
                <p className="text-sm text-muted-foreground">{employee.employeeNumber}</p>
              </div>
            </div>
            
            <div className="flex items-center gap-4">
              <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                <Calendar className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-sm font-medium">Start Date</p>
                <p className="text-sm text-muted-foreground">{new Date(employee.startDate).toLocaleDateString()}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

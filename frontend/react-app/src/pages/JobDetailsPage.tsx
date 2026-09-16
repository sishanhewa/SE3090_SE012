import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { jobsApi, type JobResponse } from '../api/jobsApi';
import { companiesApi, type CompanyResponse } from '../api/companiesApi';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ArrowLeft, Briefcase, Building2, MapPin, DollarSign, Clock, LayoutDashboard, CheckCircle2, XCircle } from 'lucide-react';

export default function JobDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [job, setJob] = useState<JobResponse | null>(null);
  const [company, setCompany] = useState<CompanyResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchDetails = async () => {
    if (!id) return;
    try {
      const jobData = await jobsApi.getById(id);
      setJob(jobData);
      if (jobData.companyId) {
        const companyData = await companiesApi.getById(jobData.companyId);
        setCompany(companyData);
      }
    } catch (error) {
      console.error('Failed to fetch job details', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDetails();
  }, [id]);

  const handlePublish = async () => {
    if (!id || !job?.companyId) return;
    if (!window.confirm('Are you sure you want to publish this job? Once published, candidates will be able to apply.')) return;
    
    try {
      await jobsApi.publish(job.companyId, id);
      fetchDetails();
    } catch (error) {
      console.error('Failed to publish job', error);
    }
  };

  const handleClose = async () => {
    if (!id || !job?.companyId) return;
    if (!window.confirm('Are you sure you want to close this job? Candidates will no longer be able to apply.')) return;
    
    try {
      await jobsApi.close(job.companyId, id);
      fetchDetails();
    } catch (error) {
      console.error('Failed to close job', error);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading job details...</div>;
  }

  if (!job) {
    return <div className="p-8 text-center text-destructive">Job not found.</div>;
  }

  return (
    <div className="p-8 space-y-6 max-w-5xl mx-auto">
      <Link to="/jobs">
        <Button variant="ghost" className="mb-4 gap-2">
          <ArrowLeft className="h-4 w-4" />
          Back to Jobs
        </Button>
      </Link>
      
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-card p-6 rounded-lg border shadow-sm">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <h2 className="text-3xl font-bold tracking-tight">{job.title}</h2>
            <Badge variant={job.status === 'Published' ? 'default' : job.status === 'Closed' ? 'destructive' : 'secondary'}>
              {job.status}
            </Badge>
          </div>
          <div className="flex flex-wrap items-center gap-4 text-muted-foreground text-sm">
            {company && (
              <Link to={`/companies/${company.id}`} className="flex items-center gap-1 hover:text-primary transition-colors">
                <Building2 className="h-4 w-4" /> {company.name}
              </Link>
            )}
            <span className="flex items-center gap-1"><MapPin className="h-4 w-4" /> {job.location}</span>
            <span className="flex items-center gap-1"><LayoutDashboard className="h-4 w-4" /> {job.department}</span>
          </div>
        </div>
        
        <div className="flex gap-2">
          {job.status === 'Draft' && (
            <>
              <Link to={`/jobs/${job.id}/edit`}>
                <Button variant="outline" className="gap-2">
                  Edit Job
                </Button>
              </Link>
              <Button onClick={handlePublish} className="gap-2">
                <CheckCircle2 className="h-4 w-4" /> Publish Job
              </Button>
            </>
          )}
          {job.status === 'Published' && (
            <Button variant="destructive" onClick={handleClose} className="gap-2">
              <XCircle className="h-4 w-4" /> Close Job
            </Button>
          )}
          <Link to={`/applications?jobId=${job.id}`}>
            <Button variant="secondary" className="gap-2">
              View Applications
            </Button>
          </Link>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="md:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-xl">Job Description</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap text-muted-foreground">
                {job.description}
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Job Overview</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                  <Clock className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="text-sm font-medium">Employment Type</p>
                  <p className="text-sm text-muted-foreground">{job.employmentType}</p>
                </div>
              </div>
              
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                  <Briefcase className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="text-sm font-medium">Experience Level</p>
                  <p className="text-sm text-muted-foreground">{job.experienceLevel}</p>
                </div>
              </div>

              {(job.salaryMin || job.salaryMax) && (
                <div className="flex items-center gap-3">
                  <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                    <DollarSign className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">Salary Range</p>
                    <p className="text-sm text-muted-foreground">
                      {job.salaryMin ? `$${job.salaryMin.toLocaleString()}` : ''} 
                      {job.salaryMin && job.salaryMax ? ' - ' : ''} 
                      {job.salaryMax ? `$${job.salaryMax.toLocaleString()}` : ''}
                    </p>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

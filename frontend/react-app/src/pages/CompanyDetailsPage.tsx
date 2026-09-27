import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { companiesApi, type CompanyResponse } from '../api/companiesApi';
import { jobsApi, type JobResponse } from '../api/jobsApi';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ArrowLeft, Building2, MapPin, Mail, Globe, Phone, Briefcase } from 'lucide-react';

export default function CompanyDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [company, setCompany] = useState<CompanyResponse | null>(null);
  const [jobs, setJobs] = useState<JobResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDetails = async () => {
      if (!id) return;
      try {
        const [companyData, allJobs] = await Promise.all([
          companiesApi.getById(id),
          jobsApi.getAll()
        ]);
        setCompany(companyData);
        setJobs(allJobs.filter((j: any) => j.companyId === id));
      } catch (error) {
        console.error('Failed to fetch company details', error);
      } finally {
        setLoading(false);
      }
    };
    fetchDetails();
  }, [id]);

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading company details...</div>;
  }

  if (!company) {
    return <div className="p-8 text-center text-destructive">Company not found.</div>;
  }

  return (
    <div className="p-8 space-y-6">
      <Link to="/companies">
        <Button variant="ghost" className="mb-4 gap-2">
          <ArrowLeft className="h-4 w-4" />
          Back to Companies
        </Button>
      </Link>
      
      <div className="flex justify-between items-start">
        <div className="flex items-center gap-4">
          <div className="h-16 w-16 bg-primary/10 rounded-lg flex items-center justify-center">
            <Building2 className="h-8 w-8 text-primary" />
          </div>
          <div>
            <h2 className="text-3xl font-bold tracking-tight">{company.name}</h2>
            <div className="flex items-center gap-4 mt-2 text-muted-foreground">
              <Badge variant="outline">{company.industry}</Badge>
              <span className="flex items-center gap-1 text-sm"><MapPin className="h-4 w-4" /> {company.location}</span>
            </div>
          </div>
        </div>
        <Badge variant={company.isActive ? "default" : "secondary"}>
          {company.isActive ? 'Active' : 'Inactive'}
        </Badge>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="col-span-1">
          <CardHeader>
            <CardTitle className="text-lg">Contact Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3 text-sm">
              <Mail className="h-4 w-4 text-muted-foreground" />
              <a href={`mailto:${company.contactEmail}`} className="text-primary hover:underline">{company.contactEmail}</a>
            </div>
            {company.contactPhone && (
              <div className="flex items-center gap-3 text-sm">
                <Phone className="h-4 w-4 text-muted-foreground" />
                <span>{company.contactPhone}</span>
              </div>
            )}
            {company.website && (
              <div className="flex items-center gap-3 text-sm">
                <Globe className="h-4 w-4 text-muted-foreground" />
                <a href={company.website} target="_blank" rel="noreferrer" className="text-primary hover:underline">
                  {company.website.replace(/^https?:\/\//, '')}
                </a>
              </div>
            )}
          </CardContent>
        </Card>

        <Card className="col-span-1 md:col-span-2">
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-lg flex items-center gap-2">
              <Briefcase className="h-5 w-5 text-primary" />
              Job Postings ({jobs.length})
            </CardTitle>
            <Link to="/jobs">
              <Button size="sm">Post New Job</Button>
            </Link>
          </CardHeader>
          <CardContent>
            {jobs.length === 0 ? (
              <div className="text-center py-6 text-muted-foreground">
                No jobs posted for this company yet.
              </div>
            ) : (
              <div className="space-y-4">
                {jobs.map(job => (
                  <div key={job.id} className="flex justify-between items-center p-4 border rounded-lg hover:bg-muted/50 transition-colors">
                    <div>
                      <h4 className="font-semibold text-lg">{job.title}</h4>
                      <p className="text-sm text-muted-foreground">{job.department} • {job.location}</p>
                    </div>
                    <div className="flex items-center gap-3">
                      <Badge variant={job.status === 'Published' ? 'default' : 'secondary'}>{job.status}</Badge>
                      <Link to={`/jobs/${job.id}`}>
                        <Button variant="outline" size="sm">View</Button>
                      </Link>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

import { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { jobsApi, type JobResponse, type CreateJobRequest } from '../api/jobsApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { ArrowLeft, Save } from 'lucide-react';

export default function EditJobPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [job, setJob] = useState<JobResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  
  const [formData, setFormData] = useState<Partial<CreateJobRequest>>({
    title: '',
    description: '',
    department: '',
    location: '',
    employmentType: '',
    experienceLevel: '',
    salaryMin: undefined,
    salaryMax: undefined,
  });

  useEffect(() => {
    const fetchJob = async () => {
      if (!id) return;
      try {
        const jobData = await jobsApi.getById(id);
        setJob(jobData);
        setFormData({
          title: jobData.title,
          description: jobData.description,
          department: jobData.department,
          location: jobData.location,
          employmentType: jobData.employmentType,
          experienceLevel: jobData.experienceLevel,
          salaryMin: jobData.salaryMin,
          salaryMax: jobData.salaryMax,
        });
      } catch (error) {
        console.error('Failed to fetch job details', error);
      } finally {
        setLoading(false);
      }
    };
    fetchJob();
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id || !job?.companyId) return;
    setSaving(true);
    try {
      await jobsApi.update(job.companyId, id, formData);
      navigate(`/jobs/${id}`);
    } catch (error) {
      console.error('Failed to update job', error);
      alert('Failed to update job');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading job details...</div>;
  }

  if (!job) {
    return <div className="p-8 text-center text-destructive">Job not found.</div>;
  }

  return (
    <div className="p-8 space-y-6 max-w-4xl mx-auto">
      <Link to={`/jobs/${id}`}>
        <Button variant="ghost" className="mb-4 gap-2">
          <ArrowLeft className="h-4 w-4" />
          Cancel Edit
        </Button>
      </Link>
      
      <Card>
        <CardHeader>
          <CardTitle className="text-2xl">Edit Job: {job.title}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4 grid grid-cols-2 gap-4">
            <div className="col-span-2">
              <label className="text-sm font-medium mb-1 block">Job Title</label>
              <Input
                placeholder="Job Title"
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                required
              />
            </div>
            <div className="col-span-2">
              <label className="text-sm font-medium mb-1 block">Job Description</label>
              <textarea
                className="flex min-h-[150px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                placeholder="Job Description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-1 block">Department</label>
              <Input
                placeholder="Department"
                value={formData.department}
                onChange={(e) => setFormData({ ...formData, department: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-1 block">Location</label>
              <Input
                placeholder="Location"
                value={formData.location}
                onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-1 block">Employment Type</label>
              <Input
                placeholder="Employment Type (e.g. Full-time)"
                value={formData.employmentType}
                onChange={(e) => setFormData({ ...formData, employmentType: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-1 block">Experience Level</label>
              <Input
                placeholder="Experience Level"
                value={formData.experienceLevel}
                onChange={(e) => setFormData({ ...formData, experienceLevel: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-1 block">Minimum Salary</label>
              <Input
                placeholder="Minimum Salary"
                type="number"
                value={formData.salaryMin || ''}
                onChange={(e) => setFormData({ ...formData, salaryMin: Number(e.target.value) })}
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-1 block">Maximum Salary</label>
              <Input
                placeholder="Maximum Salary"
                type="number"
                value={formData.salaryMax || ''}
                onChange={(e) => setFormData({ ...formData, salaryMax: Number(e.target.value) })}
              />
            </div>
            <div className="col-span-2 pt-4 flex justify-end">
              <Button type="submit" disabled={saving} className="gap-2">
                <Save className="h-4 w-4" /> {saving ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

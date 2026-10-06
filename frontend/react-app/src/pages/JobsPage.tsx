import { useState, useEffect } from 'react';
import { jobsApi, type JobResponse, type CreateJobRequest } from '../api/jobsApi';
import { companiesApi, type CompanyResponse } from '../api/companiesApi';
import { useAuthStore } from '../store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Briefcase, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';

export default function JobsPage() {
  const { user } = useAuthStore();
  const isAdmin = user?.roles?.includes('SystemAdmin');
  const [jobs, setJobs] = useState<JobResponse[]>([]);
  const [companies, setCompanies] = useState<CompanyResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const [selectedCompanyId, setSelectedCompanyId] = useState('');
  const [requirementsText, setRequirementsText] = useState('');
  const [skillsText, setSkillsText] = useState('');
  const [educationLevel, setEducationLevel] = useState('None');

  const [formData, setFormData] = useState<CreateJobRequest>({
    title: '',
    description: '',
    departmentId: '',
    location: '',
    employmentType: 'Full-time',
    minimumExperience: 0,
    vacancyCount: 1,
    applicationDeadline: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    requirements: []
  });

  const fetchData = async () => {
    try {
      const [jobsData, companiesData] = await Promise.all([
        jobsApi.getAll(),
        companiesApi.getAll()
      ]);
      setJobs(jobsData);
      setCompanies(companiesData);
    } catch (error) {
      console.error('Failed to fetch data', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const departmentsMap = new Map<string, string>();
  jobs.forEach(j => {
    if (j.departmentId && j.departmentName) {
      departmentsMap.set(j.departmentId, j.departmentName);
    }
  });
  const uniqueDepartments = Array.from(departmentsMap.entries()).map(([id, name]) => ({ id, name }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCompanyId) {
      alert("Please select a company.");
      return;
    }

    // Parse requirements text
    const parsedReqs = requirementsText
      .split('\n')
      .map(line => line.trim())
      .filter(line => line.length > 0)
      .map(desc => ({
        description: desc,
        isMandatory: true,
        weight: 10
      }));

    // Parse skills
    const parsedSkills = skillsText
      .split(',')
      .map(skill => skill.trim())
      .filter(skill => skill.length > 0)
      .map(skill => ({
        description: `Skill: ${skill}`,
        isMandatory: true,
        weight: 15
      }));

    const requirements = [...parsedReqs, ...parsedSkills];

    if (educationLevel !== 'None') {
      requirements.push({
        description: `Minimum Education: ${educationLevel}`,
        isMandatory: true,
        weight: 10
      });
    }

    try {
      await jobsApi.create(selectedCompanyId, { ...formData, requirements });
      setIsModalOpen(false);
      setFormData({
        title: '', description: '', departmentId: '',
        location: '', employmentType: 'Full-time', minimumExperience: 0,
        vacancyCount: 1, applicationDeadline: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
        requirements: []
      });
      setRequirementsText('');
      setSkillsText('');
      setEducationLevel('None');
      fetchData();
    } catch (error: any) {
      console.error('Failed to create job', error);
      alert('Failed to create job: ' + (error.response?.data || error.message));
    }
  };

  const getCompanyName = (companyId: string) => {
    const company = companies.find(c => c.id === companyId);
    return company ? company.name : 'Unknown Company';
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Jobs</h2>
          <p className="text-muted-foreground">
            {isAdmin ? 'Manage job postings across all companies.' : 'Manage your company\'s job postings.'}
          </p>
        </div>

        <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
          <DialogTrigger asChild>
            <Button className="gap-2">
              <Plus className="h-4 w-4" />
              Post a Job
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>Create New Job Posting</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-4 grid grid-cols-2 gap-4">
              <div className="col-span-2">
                <select
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
                  value={selectedCompanyId}
                  onChange={(e) => setSelectedCompanyId(e.target.value)}
                  required
                >
                  <option value="" disabled>Select Company</option>
                  {companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
              <div className="col-span-2">
                <Input
                  placeholder="Job Title"
                  value={formData.title}
                  onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                  required
                />
              </div>
              <div className="col-span-2">
                <textarea
                  className="flex min-h-[100px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  placeholder="Job Description"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  required
                />
              </div>
              <div className="col-span-2">
                <div className="text-sm font-medium mb-1">General Requirements (One per line)</div>
                <textarea
                  className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  placeholder="e.g. Willing to relocate"
                  value={requirementsText}
                  onChange={(e) => setRequirementsText(e.target.value)}
                />
              </div>
              <div className="col-span-2">
                <div className="text-sm font-medium mb-1">Required Skills (Comma separated)</div>
                <Input
                  placeholder="e.g. React, C#, SQL, Team Leadership"
                  value={skillsText}
                  onChange={(e) => setSkillsText(e.target.value)}
                />
              </div>
              <div className="col-span-1">
                <div className="text-sm font-medium mb-1">Education Requirement</div>
                <select
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
                  value={educationLevel}
                  onChange={(e) => setEducationLevel(e.target.value)}
                >
                  <option value="None">No Minimum Education</option>
                  <option value="High School">High School</option>
                  <option value="Associate's Degree">Associate's Degree</option>
                  <option value="Bachelor's Degree">Bachelor's Degree</option>
                  <option value="Master's Degree">Master's Degree</option>
                  <option value="PhD">PhD</option>
                </select>
              </div>

              <div className="col-span-1">
                <div className="text-sm font-medium mb-1">Department</div>
                <select
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
                  value={formData.departmentId}
                  onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                  required
                >
                  <option value="" disabled>Select Department</option>
                  {uniqueDepartments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </div>
              <div className="col-span-1">
                <Input
                  placeholder="Location"
                  value={formData.location}
                  onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                  required
                />
              </div>

              <div className="col-span-1">
                <Input
                  placeholder="Employment Type (e.g. Full-time)"
                  value={formData.employmentType}
                  onChange={(e) => setFormData({ ...formData, employmentType: e.target.value })}
                  required
                />
              </div>
              <div className="col-span-1">
                <Input
                  placeholder="Minimum Experience (Years)"
                  type="number"
                  min="0"
                  value={formData.minimumExperience}
                  onChange={(e) => setFormData({ ...formData, minimumExperience: Number(e.target.value) })}
                  required
                />
              </div>

              <div className="col-span-1">
                <Input
                  placeholder="Vacancy Count"
                  type="number"
                  min="1"
                  value={formData.vacancyCount}
                  onChange={(e) => setFormData({ ...formData, vacancyCount: Number(e.target.value) })}
                  required
                />
              </div>
              <div className="col-span-1">
                <div className="text-xs text-muted-foreground mb-1">Application Deadline</div>
                <Input
                  type="date"
                  value={formData.applicationDeadline}
                  onChange={(e) => setFormData({ ...formData, applicationDeadline: e.target.value })}
                  required
                />
              </div>

              <Input
                placeholder="Minimum Salary"
                type="number"
                value={formData.salaryMin || ''}
                onChange={(e) => setFormData({ ...formData, salaryMin: Number(e.target.value) })}
              />
              <Input
                placeholder="Maximum Salary"
                type="number"
                value={formData.salaryMax || ''}
                onChange={(e) => setFormData({ ...formData, salaryMax: Number(e.target.value) })}
              />

              <div className="col-span-2 pt-2">
                <Button type="submit" className="w-full">Create Job Posting</Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Briefcase className="h-5 w-5 text-primary" />
            All Jobs
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex justify-center py-8 text-muted-foreground">Loading jobs...</div>
          ) : jobs.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground border-2 border-dashed rounded-lg">
              <Briefcase className="h-12 w-12 mb-4 opacity-50" />
              <p>No jobs posted yet.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Company</TableHead>
                  <TableHead>Department</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {jobs.map((job) => (
                  <TableRow key={job.id}>
                    <TableCell className="font-medium">{job.title}</TableCell>
                    <TableCell>{getCompanyName(job.companyId)}</TableCell>
                    <TableCell>{job.department || job.departmentName}</TableCell>
                    <TableCell>
                      <Badge variant={job.status === 'Published' ? 'default' : 'secondary'}>
                        {job.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <Link to={`/jobs/${job.id}`}>
                        <Button variant="outline" size="sm">View Details</Button>
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

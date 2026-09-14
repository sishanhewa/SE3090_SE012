export interface JobResponse {
  id: string;
  companyId: string;
  title: string;
  description: string;
  department: string;
  location: string;
  employmentType: string;
  experienceLevel: string;
  status: string;
  salaryMin?: number;
  salaryMax?: number;
  postedDate?: string;
}

import { useState, useEffect } from 'react';
import { candidateProfileApi, type CandidateProfileResponse, type UpdateCandidateProfileRequest } from '../api/candidateProfileApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { User, Briefcase, GraduationCap, Link as LinkIcon, FileText } from 'lucide-react';

export default function CandidateProfilePage() {
  const [profile, setProfile] = useState<CandidateProfileResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  
  const [formData, setFormData] = useState<UpdateCandidateProfileRequest>({
    skills: [],
    experienceYears: 0,
    educationLevel: '',
    portfolioUrl: '',
    resumeUrl: '',
  });
  const [skillsInput, setSkillsInput] = useState('');

  const fetchProfile = async () => {
    try {
      const data = await candidateProfileApi.getProfile();
      setProfile(data);
      setFormData({
        skills: data.skills || [],
        experienceYears: data.experienceYears || 0,
        educationLevel: data.educationLevel || '',
        portfolioUrl: data.portfolioUrl || '',
        resumeUrl: data.resumeUrl || '',
      });
      setSkillsInput(data.skills?.join(', ') || '');
    } catch (error) {
      console.error('Failed to fetch profile', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const parsedSkills = skillsInput.split(',').map(s => s.trim()).filter(s => s);
      await candidateProfileApi.updateProfile({ ...formData, skills: parsedSkills });
      setIsEditing(false);
      fetchProfile();
    } catch (error) {
      console.error('Failed to update profile', error);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-muted-foreground">Loading profile...</div>;
  }

  return (
    <div className="p-8 space-y-6 max-w-3xl mx-auto">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Candidate Profile</h2>
          <p className="text-muted-foreground">Manage your professional information and skills.</p>
        </div>
        {!isEditing && (
          <Button onClick={() => setIsEditing(true)}>Edit Profile</Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <User className="h-5 w-5 text-primary" />
            Profile Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isEditing ? (
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">Skills (comma separated)</label>
                <Input
                  value={skillsInput}
                  onChange={(e) => setSkillsInput(e.target.value)}
                  placeholder="e.g. React, TypeScript, C#"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Years of Experience</label>
                <Input
                  type="number"
                  value={formData.experienceYears}
                  onChange={(e) => setFormData({ ...formData, experienceYears: Number(e.target.value) })}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Education Level</label>
                <Input
                  value={formData.educationLevel}
                  onChange={(e) => setFormData({ ...formData, educationLevel: e.target.value })}
                  placeholder="e.g. Bachelor's Degree in Computer Science"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Portfolio URL</label>
                <Input
                  type="url"
                  value={formData.portfolioUrl || ''}
                  onChange={(e) => setFormData({ ...formData, portfolioUrl: e.target.value })}
                  placeholder="https://yourportfolio.com"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Resume URL</label>
                <Input
                  type="url"
                  value={formData.resumeUrl || ''}
                  onChange={(e) => setFormData({ ...formData, resumeUrl: e.target.value })}
                  placeholder="https://link-to-your-resume.pdf"
                />
              </div>
              <div className="flex gap-2 pt-4">
                <Button type="submit">Save Changes</Button>
                <Button type="button" variant="outline" onClick={() => setIsEditing(false)}>Cancel</Button>
              </div>
            </form>
          ) : (
            <div className="space-y-6">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                  <Briefcase className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="text-sm font-medium">Experience</p>
                  <p className="text-sm text-muted-foreground">{profile?.experienceYears} Years</p>
                </div>
              </div>
              
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                  <GraduationCap className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="text-sm font-medium">Education</p>
                  <p className="text-sm text-muted-foreground">{profile?.educationLevel || 'Not specified'}</p>
                </div>
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium flex items-center gap-2">
                  <FileText className="h-4 w-4 text-muted-foreground" />
                  Skills
                </p>
                <div className="flex flex-wrap gap-2">
                  {profile?.skills && profile.skills.length > 0 ? (
                    profile.skills.map(skill => (
                      <span key={skill} className="px-2 py-1 bg-secondary text-secondary-foreground text-xs rounded-md">
                        {skill}
                      </span>
                    ))
                  ) : (
                    <span className="text-sm text-muted-foreground">No skills added yet.</span>
                  )}
                </div>
              </div>

              <div className="pt-4 flex flex-col gap-3">
                {profile?.portfolioUrl && (
                  <a href={profile.portfolioUrl} target="_blank" rel="noreferrer" className="flex items-center gap-2 text-primary hover:underline text-sm">
                    <LinkIcon className="h-4 w-4" /> View Portfolio
                  </a>
                )}
                {profile?.resumeUrl && (
                  <a href={profile.resumeUrl} target="_blank" rel="noreferrer" className="flex items-center gap-2 text-primary hover:underline text-sm">
                    <FileText className="h-4 w-4" /> Download Resume
                  </a>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

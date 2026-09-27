import { useState, useEffect } from 'react';
import { offersApi, type OfferResponse, type CreateOfferRequest } from '../api/offersApi';
import { applicationsApi, type ApplicationResponse } from '../api/applicationsApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { FileText, Plus, CheckCircle, Send } from 'lucide-react';

export default function OffersPage() {
  const [offers, setOffers] = useState<OfferResponse[]>([]);
  const [applications, setApplications] = useState<ApplicationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  
  const [formData, setFormData] = useState<CreateOfferRequest>({
    applicationId: '',
    position: '',
    salary: 0,
    employmentType: 'Full-Time',
    startDate: '',
    expiryDate: '',
    additionalTerms: ''
  });

  const fetchData = async () => {
    try {
      const [offersData, applicationsData] = await Promise.all([
        offersApi.getAll(),
        applicationsApi.getAll()
      ]);
      setOffers(offersData);
      setApplications(applicationsData);
    } catch (error) {
      console.error('Failed to fetch data', error);
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
      await offersApi.create({
        ...formData,
        startDate: new Date(formData.startDate).toISOString(),
        expiryDate: new Date(formData.expiryDate).toISOString()
      });
      setIsModalOpen(false);
      setFormData({ 
        applicationId: '', position: '', salary: 0, employmentType: 'Full-Time',
        startDate: '', expiryDate: '', additionalTerms: '' 
      });
      fetchData();
    } catch (error) {
      console.error('Failed to create offer', error);
      alert('Failed to create offer.');
    }
  };

  const handleUpdateStatus = async (id: string, status: string) => {
    try {
      await offersApi.updateStatus(id, status);
      fetchData();
    } catch (error) {
      console.error('Failed to update status', error);
    }
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Offers</h2>
          <p className="text-muted-foreground">Manage and track candidate job offers.</p>
        </div>
        
        <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
          <DialogTrigger asChild>
            <Button className="gap-2">
              <Plus className="h-4 w-4" />
              Create Offer
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-xl">
            <DialogHeader>
              <DialogTitle>Draft a New Offer</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-4">
              <select 
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.applicationId}
                onChange={(e) => {
                  const appId = e.target.value;
                  const app = applications.find(a => a.id === appId);
                  setFormData({ 
                    ...formData, 
                    applicationId: appId,
                    position: app?.jobTitle || ''
                  });
                }}
                required
              >
                <option value="" disabled>Select Application</option>
                {applications.filter(a => a.status === 'Interview' || a.status === 'Shortlisted').map(app => (
                  <option key={app.id} value={app.id}>
                    {app.jobTitle} - {app.candidateName} ({app.status})
                  </option>
                ))}
              </select>
              
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">Position</label>
                  <Input
                    type="text"
                    value={formData.position}
                    onChange={(e) => setFormData({ ...formData, position: e.target.value })}
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Salary (Annual)</label>
                  <Input
                    type="number"
                    value={formData.salary || ''}
                    onChange={(e) => setFormData({ ...formData, salary: Number(e.target.value) })}
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="text-sm font-medium">Employment Type</label>
                  <select 
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={formData.employmentType}
                    onChange={(e) => setFormData({ ...formData, employmentType: e.target.value })}
                  >
                    <option value="Full-Time">Full-Time</option>
                    <option value="Part-Time">Part-Time</option>
                    <option value="Contract">Contract</option>
                  </select>
                </div>
                <div>
                  <label className="text-sm font-medium">Start Date</label>
                  <Input
                    type="date"
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Expiry Date</label>
                  <Input
                    type="date"
                    value={formData.expiryDate}
                    onChange={(e) => setFormData({ ...formData, expiryDate: e.target.value })}
                    required
                  />
                </div>
              </div>

              <div>
                <label className="text-sm font-medium">Additional Terms</label>
                <textarea 
                  className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={formData.additionalTerms}
                  onChange={e => setFormData({...formData, additionalTerms: e.target.value})}
                  placeholder="Bonus structure, benefits, etc."
                />
              </div>
              
              <Button type="submit" className="w-full">Create Offer Draft</Button>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-primary" />
            All Offers
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex justify-center py-8 text-muted-foreground">Loading offers...</div>
          ) : offers.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground border-2 border-dashed rounded-lg">
              <FileText className="h-12 w-12 mb-4 opacity-50" />
              <p>No offers have been created yet.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Candidate</TableHead>
                  <TableHead>Position</TableHead>
                  <TableHead>Salary</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {offers.map((offer) => (
                  <TableRow key={offer.id}>
                    <TableCell className="font-medium">
                      {offer.candidateName}
                    </TableCell>
                    <TableCell>{offer.position}</TableCell>
                    <TableCell>${offer.salary.toLocaleString()}</TableCell>
                    <TableCell>
                      <Badge variant={
                        offer.status === 'Accepted' ? 'default' : 
                        offer.status === 'Rejected' || offer.status === 'Withdrawn' ? 'destructive' : 
                        offer.status === 'Sent' ? 'secondary' : 'outline'
                      }>
                        {offer.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right space-x-2">
                      {offer.status === 'Draft' && (
                        <Button variant="ghost" size="sm" onClick={() => handleUpdateStatus(offer.id, 'Sent')} title="Send to Candidate">
                          <Send className="h-4 w-4 text-blue-500" />
                        </Button>
                      )}
                      {offer.status === 'Sent' && (
                        <>
                          <Button variant="ghost" size="sm" onClick={() => handleUpdateStatus(offer.id, 'Accepted')} title="Mark Accepted">
                            <CheckCircle className="h-4 w-4 text-green-500" />
                          </Button>
                        </>
                      )}
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

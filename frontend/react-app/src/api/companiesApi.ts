export interface CompanyResponse {
  id: string;
  name: string;
}

export const companiesApi = {
  getAll: async () => {
    return [] as CompanyResponse[];
  }
};

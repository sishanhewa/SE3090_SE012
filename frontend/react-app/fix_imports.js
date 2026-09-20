const fs = require('fs');
const glob = require('glob');

const apiFiles = glob.sync('src/api/*Api.ts');
apiFiles.forEach(file => {
  let content = fs.readFileSync(file, 'utf8');
  if (content.includes('PagedResult<') && !content.includes('PagedResult')) {
    // Wait, it might include PagedResult but not import it.
    if (!content.includes("import { PagedResult }") && !content.includes("import type { PagedResult }") && !content.includes("import apiClient, { PagedResult }")) {
      content = content.replace("import apiClient from './apiClient';", "import apiClient, { PagedResult } from './apiClient';");
      fs.writeFileSync(file, content);
      console.log('Fixed imports in', file);
    }
  }
});

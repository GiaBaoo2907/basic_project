using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DeviceManagement.Data;
using DeviceManagement.Models;
using System.Data.Entity;

namespace DeviceManagement.Services
{
    public class RequestService : IDisposable
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public IQueryable<Request> Query() => _db.Requests.Where(x => !x.IsDeleted);

        public IQueryable<Request> QueryDeleted() => _db.Requests.Where(x => x.IsDeleted);

        public async Task<RequestForm> SaveFormAsync(RequestForm form)
        {
            if (form.Assets == null)
                form.Assets = new System.Collections.Generic.List<RequestAssetForm>();

            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                Request entity;
                if (form.Id == 0)
                {
                    entity = new Request
                    {
                        Code = GenerateCode(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "web"
                    };
                    _db.Requests.Add(entity);
                }
                else
                {
                    entity = await _db.Requests.FirstOrDefaultAsync(x => x.Id == form.Id && !x.IsDeleted);
                    if (entity == null)
                        return null;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.UpdatedBy = "web";
                }

                entity.Title = form.Title.Trim();
                entity.Creator = form.Creator.Trim();
                entity.DepartmentProject = form.DepartmentProject.Trim();
                entity.Classification = string.IsNullOrWhiteSpace(form.Classification) ? null : form.Classification.Trim();
                entity.InsuranceType = string.IsNullOrWhiteSpace(form.InsuranceType) ? null : form.InsuranceType.Trim();
                entity.InsuranceExpiry = form.InsuranceExpiry;
                entity.CreatedDate = form.CreatedDate.Value;

                await _db.SaveChangesAsync();

                var oldAssets = _db.RequestAssets.Where(x => x.RequestId == entity.Id).ToList();
                var retainedAssetIds = form.Assets.Where(x => x.Id > 0).Select(x => x.Id).ToList();
                var removedAssetIds = oldAssets.Where(x => !retainedAssetIds.Contains(x.Id)).Select(x => x.Id).ToList();
                if (removedAssetIds.Count > 0)
                {
                    var attachments = _db.RequestAttachments.Where(x => x.RequestId == entity.Id && x.RequestAssetId.HasValue).ToList();
                    foreach (var attachment in attachments.Where(x => removedAssetIds.Contains(x.RequestAssetId.Value)))
                        attachment.RequestAssetId = null;
                    _db.RequestAssets.RemoveRange(oldAssets.Where(x => removedAssetIds.Contains(x.Id)));
                }
                var savedAssets = new System.Collections.Generic.List<RequestAsset>();
                foreach (var asset in form.Assets)
                {
                    var savedAsset = oldAssets.FirstOrDefault(x => x.Id == asset.Id);
                    if (savedAsset == null)
                    {
                        savedAsset = new RequestAsset { RequestId = entity.Id };
                        _db.RequestAssets.Add(savedAsset);
                    }
                    savedAsset.Equipment = asset.Equipment.Trim();
                    savedAsset.DocumentName = string.IsNullOrWhiteSpace(asset.DocumentName) ? null : asset.DocumentName.Trim();
                    savedAsset.Supplier = string.IsNullOrWhiteSpace(asset.Supplier) ? null : asset.Supplier.Trim();
                    savedAsset.PurchaseDate = asset.PurchaseDate.Value;
                    savedAsset.ExpiryDate = asset.ExpiryDate;
                    savedAsset.Value = asset.Value;
                    savedAsset.Premium = asset.Premium;
                    savedAsset.Quantity = asset.Quantity;
                    savedAsset.InsuranceType = string.IsNullOrWhiteSpace(asset.InsuranceType) ? null : asset.InsuranceType.Trim();
                    savedAsset.ContractNumber = asset.ContractNumber.Trim();
                    savedAssets.Add(savedAsset);
                }

                await _db.SaveChangesAsync();
                transaction.Commit();

                form.Id = entity.Id;
                form.Code = entity.Code;
                form.Assets = savedAssets.Select(x => new RequestAssetForm
                    {
                        Id = x.Id,
                        Equipment = x.Equipment,
                        DocumentName = x.DocumentName,
                        Supplier = x.Supplier,
                        PurchaseDate = x.PurchaseDate,
                        ExpiryDate = x.ExpiryDate,
                        Value = x.Value,
                        Premium = x.Premium,
                        Quantity = x.Quantity,
                        InsuranceType = x.InsuranceType,
                        ContractNumber = x.ContractNumber
                    }).ToList();

                return form;
            }
        }

        public async Task<RequestForm> GetFormAsync(int id)
        {
            var entity = await _db.Requests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (entity == null)
                return null;

            return new RequestForm
            {
                Id = entity.Id,
                Code = entity.Code,
                Title = entity.Title,
                Creator = entity.Creator,
                DepartmentProject = entity.DepartmentProject,
                Classification = entity.Classification,
                InsuranceType = entity.InsuranceType,
                InsuranceExpiry = entity.InsuranceExpiry,
                CreatedDate = entity.CreatedDate,
                Assets = await _db.RequestAssets.Where(x => x.RequestId == id)
                    .Select(x => new RequestAssetForm
                    {
                        Id = x.Id,
                        Equipment = x.Equipment,
                        DocumentName = x.DocumentName,
                        Supplier = x.Supplier,
                        PurchaseDate = x.PurchaseDate,
                        ExpiryDate = x.ExpiryDate,
                        Value = x.Value,
                        Premium = x.Premium,
                        Quantity = x.Quantity,
                        InsuranceType = x.InsuranceType,
                        ContractNumber = x.ContractNumber
                    }).ToListAsync()
            };
        }

        private string GenerateCode()
        {
            var year = DateTime.UtcNow.AddHours(7).Year;
            var prefix = "TBBH" + year + "-";
            var codes = _db.Requests.Where(x => x.Code.StartsWith(prefix)).Select(x => x.Code).ToList();
            var maxNumber = 0;
            foreach (var code in codes)
            {
                int number;
                if (int.TryParse(code.Substring(prefix.Length), out number) && number > maxNumber)
                    maxNumber = number;
            }
            return prefix + (maxNumber + 1).ToString("D3");
        }

        public async Task<Request> GetByIdAsync(int id)
        {
            return await _db.Requests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public IQueryable<Request> GetPagedAsync(int skip, int take, string filter = null, int? year = null, string sortField = null, string sortOrder = "desc")
        {
            var query = Query();

            // Filter by text (Code, Title, Creator)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(f) ||
                    x.Title.ToLower().Contains(f) ||
                    x.Creator.ToLower().Contains(f)
                );
            }

            // Filter by year
            if (year.HasValue)
            {
                query = query.Where(x => x.CreatedDate.Year == year.Value);
            }

            // Sorting
            if (!string.IsNullOrEmpty(sortField))
            {
                bool desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
                switch (sortField)
                {
                    case "CreatedDate":
                        query = desc ? query.OrderByDescending(x => x.CreatedDate) : query.OrderBy(x => x.CreatedDate);
                        break;
                    case "Code":
                        query = desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code);
                        break;
                    case "Title":
                        query = desc ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title);
                        break;
                    default:
                        query = query.OrderByDescending(x => x.CreatedDate);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.CreatedDate);
            }

            return query.Skip(skip).Take(take);
        }

        public async Task<Request> CreateAsync(Request entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            _db.Requests.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(Request entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.Requests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (entity == null) return false;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = "web";
            entity.UpdatedAt = entity.DeletedAt;
            entity.UpdatedBy = entity.DeletedBy;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var entity = await _db.Requests.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);
            if (entity == null) return false;
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedBy = null;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = "web";
            await _db.SaveChangesAsync();
            return true;
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}

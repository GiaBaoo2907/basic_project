using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DeviceManagement.Data;
using DeviceManagement.Models;
using System.Data.Entity;

namespace DeviceManagement.Services
{
    public class InsuranceService : IDisposable
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public IQueryable<Insurance> Query() => _db.Insurances.Where(x => !x.IsDeleted);

        public IQueryable<Insurance> QueryDeleted() => _db.Insurances.Where(x => x.IsDeleted);

        public async Task<InsuranceForm> SaveFormAsync(InsuranceForm form)
        {
            if (form.Assets == null)
                form.Assets = new System.Collections.Generic.List<InsuranceAssetForm>();

            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                Insurance entity;
                if (form.Id == 0)
                {
                    entity = new Insurance
                    {
                        Code = GenerateCode(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "web"
                    };
                    _db.Insurances.Add(entity);
                }
                else
                {
                    entity = await _db.Insurances.FirstOrDefaultAsync(x => x.Id == form.Id && !x.IsDeleted);
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

                var oldAssets = _db.InsuranceAssets.Where(x => x.InsuranceId == entity.Id).ToList();
                var retainedAssetIds = form.Assets.Where(x => x.Id > 0).Select(x => x.Id).ToList();
                var removedAssetIds = oldAssets.Where(x => !retainedAssetIds.Contains(x.Id)).Select(x => x.Id).ToList();
                if (removedAssetIds.Count > 0)
                {
                    var attachments = _db.InsuranceAttachments.Where(x => x.InsuranceId == entity.Id && x.InsuranceAssetId.HasValue).ToList();
                    foreach (var attachment in attachments.Where(x => removedAssetIds.Contains(x.InsuranceAssetId.Value)))
                        attachment.InsuranceAssetId = null;
                    _db.InsuranceAssets.RemoveRange(oldAssets.Where(x => removedAssetIds.Contains(x.Id)));
                }
                var savedAssets = new System.Collections.Generic.List<InsuranceAsset>();
                foreach (var asset in form.Assets)
                {
                    var savedAsset = oldAssets.FirstOrDefault(x => x.Id == asset.Id);
                    if (savedAsset == null)
                    {
                        savedAsset = new InsuranceAsset { InsuranceId = entity.Id };
                        _db.InsuranceAssets.Add(savedAsset);
                    }
                    savedAsset.Equipment = asset.Equipment.Trim();
                    savedAsset.MaterialCode = asset.MaterialCode.Trim();
                    savedAsset.DocumentName = string.IsNullOrWhiteSpace(asset.DocumentName) ? null : asset.DocumentName.Trim();
                    savedAsset.Supplier = string.IsNullOrWhiteSpace(asset.Supplier) ? null : asset.Supplier.Trim();
                    savedAsset.PurchaseDate = asset.PurchaseDate.Value;
                    savedAsset.ExpiryDate = asset.ExpiryDate;
                    savedAsset.Value = asset.Value;
                    savedAsset.Premium = asset.Premium;
                    savedAsset.Quantity = asset.Quantity;
                    savedAsset.UnitOfMeasure = string.IsNullOrWhiteSpace(asset.UnitOfMeasure) ? null : asset.UnitOfMeasure.Trim();
                    savedAsset.InsuranceType = string.IsNullOrWhiteSpace(asset.InsuranceType) ? null : asset.InsuranceType.Trim();
                    savedAsset.ContractNumber = asset.ContractNumber.Trim();
                    savedAssets.Add(savedAsset);
                }

                await _db.SaveChangesAsync();
                transaction.Commit();

                form.Id = entity.Id;
                form.Code = entity.Code;
                form.Assets = savedAssets.Select(x => new InsuranceAssetForm
                    {
                        Id = x.Id,
                        Equipment = x.Equipment,
                        MaterialCode = x.MaterialCode,
                        DocumentName = x.DocumentName,
                        Supplier = x.Supplier,
                        PurchaseDate = x.PurchaseDate,
                        ExpiryDate = x.ExpiryDate,
                        Value = x.Value,
                        Premium = x.Premium,
                        Quantity = x.Quantity,
                        UnitOfMeasure = x.UnitOfMeasure,
                        InsuranceType = x.InsuranceType,
                        ContractNumber = x.ContractNumber
                    }).ToList();

                return form;
            }
        }

        public async Task<InsuranceForm> GetFormAsync(int id)
        {
            var entity = await _db.Insurances.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (entity == null)
                return null;

            return new InsuranceForm
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
                Assets = await _db.InsuranceAssets.Where(x => x.InsuranceId == id)
                    .Select(x => new InsuranceAssetForm
                    {
                        Id = x.Id,
                        Equipment = x.Equipment,
                        MaterialCode = x.MaterialCode,
                        DocumentName = x.DocumentName,
                        Supplier = x.Supplier,
                        PurchaseDate = x.PurchaseDate,
                        ExpiryDate = x.ExpiryDate,
                        Value = x.Value,
                        Premium = x.Premium,
                        Quantity = x.Quantity,
                        UnitOfMeasure = x.UnitOfMeasure,
                        InsuranceType = x.InsuranceType,
                        ContractNumber = x.ContractNumber
                    }).ToListAsync()
            };
        }

        private string GenerateCode()
        {
            var year = DateTime.UtcNow.AddHours(7).Year;
            var prefix = "TBBH" + year + "-";
            var codes = _db.Insurances.Where(x => x.Code.StartsWith(prefix)).Select(x => x.Code).ToList();
            var maxNumber = 0;
            foreach (var code in codes)
            {
                int number;
                if (int.TryParse(code.Substring(prefix.Length), out number) && number > maxNumber)
                    maxNumber = number;
            }
            return prefix + (maxNumber + 1).ToString("D3");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.Insurances.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
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
            var entity = await _db.Insurances.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);
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

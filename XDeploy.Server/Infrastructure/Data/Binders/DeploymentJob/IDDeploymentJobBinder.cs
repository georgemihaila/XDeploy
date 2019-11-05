using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XDeploy.Server.Infrastructure.Data.Extensions;

namespace XDeploy.Server.Infrastructure.Data
{
    public class IDDeploymentJobBinder : IModelBinder
    {
        private readonly ApplicationDbContext _context;

        public IDDeploymentJobBinder(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }
            
            var modelName = bindingContext.ModelName;
            // Try to fetch the value of the argument by name
            var valueProviderResult =
                bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName,
                valueProviderResult);

            var value = valueProviderResult.FirstValue;

            // Check if the argument value is null or empty
            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            var model = _context.DeploymentJobs.Find(value);
            if (model != null)
            {
                model.ExpectedFiles = _context.ExpectedFile.Where(x => x.ParentJob.ID == model.ID).ToList();
            }
            bindingContext.Result = ModelBindingResult.Success(model);
            return Task.CompletedTask;
        }
    }
}

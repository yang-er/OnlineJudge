using JudgeWeb.Areas.Api.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Designs
{
    public class JudgingRunBinder : IModelBinder
    {
        static readonly JsonSerializer jd = JsonSerializer.CreateDefault();

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelName = bindingContext.ModelName;

            // Try to fetch the value of the argument by name
            var valueProviderResult =
                bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(modelName,
                valueProviderResult);
            var value = valueProviderResult.FirstValue;

            // Check if the argument value is null or empty
            if (string.IsNullOrEmpty(value))
                return Task.CompletedTask;

            try
            {
                using (var sr = new StringReader(value))
                using (var jtr = new JsonTextReader(sr))
                {
                    var result = jd.Deserialize<List<JudgingRunModel>>(jtr);
                    if (result != null)
                        bindingContext.Result = ModelBindingResult.Success(result);
                }
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    "Parse error: " + ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}

using System.Text;
using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Expressions;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process
{
    /// <summary>
    /// Class implementing <see cref="IProcessExclusiveGateway" /> for evaluating expressions on flows connected to a gateway
    /// </summary>
    public class ExpressionsExclusiveGateway : IProcessExclusiveGateway
    {
        private readonly LayoutEvaluatorStateInitializer _layoutStateInit;
        private readonly IAppResources _resources;
        private readonly IAppMetadata _appMetadata;
        private readonly IData _dataClient;
        private readonly IAppModel _appModel;

        /// <summary>
        /// Constructor for <see cref="ExpressionsExclusiveGateway" />
        /// </summary>
        /// <param name="layoutEvaluatorStateInitializer"></param>
        /// <param name="resources"></param>
        /// <param name="appModel"></param>
        /// <param name="appMetadata"></param>
        /// <param name="dataClient"></param>
        public ExpressionsExclusiveGateway(
            LayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer,
            IAppResources resources,
            IAppModel appModel,
            IAppMetadata appMetadata,
            IData dataClient)
        {
            _layoutStateInit = layoutEvaluatorStateInitializer;
            _resources = resources;
            _appMetadata = appMetadata;
            _dataClient = dataClient;
            _appModel = appModel;
        }

        /// <inheritdoc />
        public string GatewayId { get; } = "AltinnExpressionsExclusiveGateway";

        /// <inheritdoc />
        public async Task<List<SequenceFlow>> FilterAsync(List<SequenceFlow> outgoingFlows, Instance instance, string? action)
        {
            var state = await GetLayoutEvaluatorState(instance, action);
            List<SequenceFlow> filteredList = new();
            foreach (var outgoingFlow in outgoingFlows)
            {
                if(EvaluateSequenceFlow(state, outgoingFlow))
                {
                    filteredList.Add(outgoingFlow);
                }
            }

            return filteredList;
        }

        private async Task<LayoutEvaluatorState> GetLayoutEvaluatorState(Instance instance, string? action)
        {
            var layoutSet = GetLayoutSet(instance);
            var dataType = await GetDataType(instance, layoutSet);
            object data = new object();
            if (dataType != null)
            {
                InstanceIdentifier instanceIdentifier = new InstanceIdentifier(instance);
                var dataGuid = GetDataId(instance, dataType.Item1);
                Type dataElementType = dataType.Item2;
                if (dataGuid != null)
                {
                    data = await _dataClient.GetFormData(instanceIdentifier.InstanceGuid, dataElementType, instance.Org, instance.AppId.Split("/")[1], int.Parse(instance.InstanceOwner.PartyId), dataGuid.Value);
                }
            }

            var state = await _layoutStateInit.Init(instance, data, layoutSetId: layoutSet?.Id, gatewayAction: action);
            return state;
        }

        private bool EvaluateSequenceFlow(LayoutEvaluatorState state, SequenceFlow sequenceFlow)
        {
            if (sequenceFlow.ConditionExpression != null)
            {
                var expression = GetExpressionFromCondition(sequenceFlow.ConditionExpression);
                foreach (var componentContext in state.GetComponentContexts())
                {
                    var result = ExpressionEvaluator.EvaluateExpression(state, expression, componentContext);
                    if (result is bool boolResult && boolResult)
                    {
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        private Expression GetExpressionFromCondition(string condition)
        {
            JsonSerializerOptions options = new()
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true,
            };
            Utf8JsonReader reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(condition));
            reader.Read();
            var expressionFromCondition = ExpressionConverter.ReadNotNull(ref reader, options);
            return expressionFromCondition;
        }

        private LayoutSet? GetLayoutSet(Instance instance)
        {
            string taskId = instance.Process.CurrentTask.ElementId;
            JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


            string layoutSetsString = _resources.GetLayoutSets();
            LayoutSet? layoutSet = null;
            if (!string.IsNullOrEmpty(layoutSetsString))
            {
                LayoutSets? layoutSets = JsonSerializer.Deserialize<LayoutSets>(layoutSetsString, options)!;
                layoutSet = layoutSets?.Sets?.FirstOrDefault(t => t.Tasks.Contains(taskId));
            }

            return layoutSet;
        }

        private async Task<Tuple<string, Type>?> GetDataType(Instance instance, LayoutSet? layoutSet)
        {
            DataType? dataTypeClassRef = null;
            if (layoutSet != null)
            {
                dataTypeClassRef = (await _appMetadata.GetApplicationMetadata()).DataTypes.FirstOrDefault(d => d.Id == layoutSet.DataType && d.AppLogic != null);
            }
            else
            {
                dataTypeClassRef = (await _appMetadata.GetApplicationMetadata()).DataTypes.FirstOrDefault(d => d.TaskId == instance.Process.CurrentTask.ElementId && d.AppLogic != null);
            }

            if (dataTypeClassRef != null)
            {
                return new Tuple<string, Type>(dataTypeClassRef.Id, _appModel.GetModelType(dataTypeClassRef.AppLogic.ClassRef));
            }

            return null;
        }

        private static Guid? GetDataId(Instance instance, string dataType)
        {
            string? dataId = instance.Data.FirstOrDefault(d => d.DataType == dataType)?.Id;
            if (dataId != null)
            {
                return new Guid(dataId);
            }

            return null;
        }
    }
}


namespace FlowWebService.Interface
{
    interface IAfterStepAudited
    {
        /// <summary>
        /// 当前节点点击pass之后的回调
        /// </summary>
        /// <param name="step">当前节点</param>
        /// <param name="stepName">当前节点名称</param>
        /// <param name="formJson">表单json</param>
        void AfterStepSucceedAudited(int? step, string stepName, string formJson);
    }
}

using System;
using System.Collections.Generic;
using PDX_CSharp.Core.Interfaces;

namespace PDX_CSharp.Templates
{
    /// <summary>
    /// 图样模板注册表（Template Registry）。
    ///
    /// 作用：集中管理所有已注册的图样模板，按 TemplateId 查找并实例化。
    ///
    /// 设计原则（支持插件化扩展）：
    ///   - 通过 Register(id, factory) 注册模板工厂函数，而非直接 new 实例，
    ///     为未来 DI 容器 / 反射加载 / 热加载模板预留扩展点。
    ///   - 内置模板在静态构造器中自动注册。
    ///   - 外部代码可调用 Register 注册自定义模板（无需修改注册表）。
    /// </summary>
    public class TemplateRegistry
    {
        // 注册表：模板ID → 工厂函数
        private readonly Dictionary<string, Func<IDiagramTemplate>> _factories
            = new Dictionary<string, Func<IDiagramTemplate>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 默认注册表（含所有内置模板），单例供全局访问。
        /// </summary>
        public static readonly TemplateRegistry Default = CreateDefault();

        // ── 构造与注册 ────────────────────────────────────────────────────

        public TemplateRegistry() { }

        /// <summary>
        /// 注册模板工厂函数。
        /// 使用工厂函数而非预构建实例，确保每次 Resolve 返回独立对象。
        /// </summary>
        /// <param name="templateId">模板唯一ID（大小写不敏感）</param>
        /// <param name="factory">工厂函数（返回一个实现 IDiagramTemplate 的新实例）</param>
        public void Register(string templateId, Func<IDiagramTemplate> factory)
        {
            if (string.IsNullOrEmpty(templateId))
                throw new ArgumentNullException("templateId");
            if (factory == null)
                throw new ArgumentNullException("factory");
            _factories[templateId] = factory;
        }

        /// <summary>
        /// 移除已注册的模板（热卸载场景）。
        /// </summary>
        public void Unregister(string templateId)
        {
            if (_factories.ContainsKey(templateId))
                _factories.Remove(templateId);
        }

        // ── 解析 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 按 TemplateId 解析并返回一个新模板实例。
        /// 找不到时返回 null（调用方按需处理）。
        /// </summary>
        public IDiagramTemplate Resolve(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return null;
            Func<IDiagramTemplate> factory;
            if (_factories.TryGetValue(templateId, out factory))
                return factory();
            return null;
        }

        /// <summary>
        /// 检查 TemplateId 是否已注册。
        /// </summary>
        public bool HasTemplate(string templateId)
        {
            return !string.IsNullOrEmpty(templateId) &&
                   _factories.ContainsKey(templateId);
        }

        /// <summary>
        /// 返回所有已注册的模板 ID 列表（供 UI 列表展示）。
        /// </summary>
        public IEnumerable<string> GetRegisteredIds()
        {
            return _factories.Keys;
        }

        // ── 默认注册表工厂 ────────────────────────────────────────────────

        private static TemplateRegistry CreateDefault()
        {
            var reg = new TemplateRegistry();
            // 内置模板注册（使用工厂函数，每次 Resolve 返回新实例）
            reg.Register("vertical_bus", () => new VerticalBusTemplate());
            reg.Register("dual_bus",     () => new DualBusTemplate());
            return reg;
        }
    }
}

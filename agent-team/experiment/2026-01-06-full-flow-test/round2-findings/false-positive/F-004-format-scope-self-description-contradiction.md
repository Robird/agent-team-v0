# F-004: rbf-format 的“本文件不定义 Address64 等接口类型”与后文章节命名易产生自我矛盾

**位置**: atelia/docs/Rbf/rbf-format.md §1 “本文档不定义”列表 vs §7 “Address64（编码层）”
**描述**: §1 声明本文档“不定义 `FrameTag`/`Address64` 等接口类型（见 rbf-interface.md）”，但 §7 又以“Address64（编码层）”为标题并给出编码/空值/对齐规则。虽然 §7 的内容实际是在定义 wire encoding（而非接口类型），但从文档自描述角度容易被读者理解为“前后矛盾”。
**建议**: 保持术语与章节命名一致以避免歧义：
- 将 §1 的表述改为“不定义接口层类型定义（C# 类型/封装），只定义 wire encoding”；或
- 将 §7 标题改为“Address64 Wire Encoding / Address64 在线格式编码”，并在首句强调“这是编码规则，不是接口类型定义”。


> ⚠️ **更名通知（2025-12-21）**：DurableHeap 已更名为 **StateJournal** 并迁入 `atelia/docs/StateJournal/`。本文档保留为历史参考。
>
> - 核心文档：`atelia/docs/StateJournal/mvp-design-v2.md`
> - Backlog：`atelia/docs/StateJournal/backlog.md`

## 动机
我们的[自研Agent](atelia/prototypes/Agent.Core) 目前在持久存储层方面依然悬而未决，需要一种append-only的object持久化技术。
我们的Agent是常年运行的，为了处理相对频繁的偶发LLM调用失败(例如网络拥堵或服务器过载)，系统正常的重启需求，将Agent实现为了可持久化状态机。
为了改善LLM对[DocUI](DocUI/docs/key-notes)的使用体验, 引入了[Micro-Wizard](DocUI/docs/key-notes/micro-wizard.md) 的概念，这其实是将工具和[App-For-LLM](DocUI/docs/key-notes/app-for-llm.md)也状态机化了，也需要可持久化。
综上，其实我们是在寻求构建一种可持久化的“进程”，或者按GPT-5.2的说法叫[Durable Workflow](agent-team/meeting/2025-12-15-tool-as-command-jam.md#Durable)。结合Elm TEA对State的需求和Event-Sourcing的需求，其实我们是需要一种“在磁盘上分配的对象”，而这在工程上可以用一组成熟模式的组合来做到。

## 设想
在函数式编程和Elm TEA思想的基础上，显式区分持久状态和临时状态，临时状态用来满足对Buffer/Cache/Index的需求，临时状态必须可以从持久状态重建，这其实是兼顾了性能现实、理论上的程序简单性、程序状态可恢复功能。

关键技术组合：Memory-Mapped-File, Copy on Write, BTree, Persist-Pointer, Lazy Wrapper, POJO/POCO, JSON, Immutable+Builder, WeakReference, ConditionalWeakTable

在API一侧，提供一组JSON风格的可组合使用的Immutable类型，本质是对底层非托管内存指针的Wrapper，是Lazy创建的。
在底层实现一侧：
  **Chunk-File**: 最底层的实际存储是无类型的一组chunk文件，构成逻辑上的一种Heap或Bytes Pool。
  **MemMap-Pool**: 在多个Chunk-File的基础上，用memmap映射为内存地址空间池。
  **BTree**: 定义一种Persist-Pointer，来使得chunk文件的特定偏移量可被持久引用。定义所支持的基元数据类型，使得Bytes可以被按类型读写。进而构建COW BTree。
  **Runtime**: 管理所有资源，提供基础功能，最终暴露为类型安全和无需手动释放的JSON风格Wrapper。

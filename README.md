# LZH.RedisMQ

本项目是基于[CAP](https://github.com/dotnetcore/CAP)定制化开发的，主要目的：

1. 去掉CAP持久化部分逻辑，避免数据库成为Redis的性能瓶颈
2. 由于没有了持久化，需要进一步完善重试、客户掉线消息转移机制
3. 根据自身项目需要进行其它定制修改






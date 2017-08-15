﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Util.Datas.Ef;
using Util.Datas.Tests.Samples.Datas.SqlServer.UnitOfWorks;
using Util.Datas.Tests.Samples.Domains.Models;
using Util.Datas.Tests.Samples.Domains.Repositories;
using Util.Datas.Tests.SqlServer.Confis;
using Util.Helpers;
using Xunit;

namespace Util.Datas.Tests.SqlServer.Tests {
    /// <summary>
    /// 商品仓储测试
    /// </summary>
    public class ProductRepositoryTest : IDisposable {
        /// <summary>
        /// 容器
        /// </summary>
        private readonly Util.DependencyInjection.IContainer _container;
        /// <summary>
        /// 工作单元
        /// </summary>
        private readonly ISqlServerUnitOfWork _unitOfWork;
        /// <summary>
        /// 商品仓储
        /// </summary>
        private readonly IProductRepository _productRepository;
        /// <summary>
        /// 随机数操作
        /// </summary>
        private readonly Util.Helpers.Random _random;

        /// <summary>
        /// 测试初始化
        /// </summary>
        public ProductRepositoryTest() {
            _container = Ioc.CreateContainer( new IocConfig() );
            _unitOfWork = _container.Create<ISqlServerUnitOfWork>();
            _productRepository = _container.Create<IProductRepository>();
            _random = new Util.Helpers.Random();
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        public void Dispose() {
            _container.Dispose();
        }

        /// <summary>
        /// 测试添加
        /// </summary>
        [Fact]
        public void TestAdd() {
            int id = _random.Next( 999999999 );
            var product = new Product( id ) { Name = "Name", Code = "Code" };
            product.ProductType = new ProductType( "Type", new List<ProductProperty>() { new ProductProperty( "A", "1" ), new ProductProperty( "B", "2" ) } );
            _productRepository.Add( product );
            _unitOfWork.Commit();

            Product result = _productRepository.GetById( id );
            Assert.Equal( id, result.Id );
            Assert.Equal( "Type", result.ProductType.Name );
            Assert.Equal( "2", result.ProductType.Properties.ToList()[1].Value );
        }

        /// <summary>
        /// 测试异步添加
        /// </summary>
        [Fact]
        public async Task TestAddAsync() {
            int id = _random.Next( 999999999 );
            var product = new Product( id ) { Name = "Name", Code = "Code" };
            product.ProductType = new ProductType( "Type", new List<ProductProperty>() { new ProductProperty( "A", "1" ), new ProductProperty( "B", "2" ) } );
            await _productRepository.AddAsync( product );
            await _unitOfWork.CommitAsync();

            Product result = _productRepository.GetById( id );
            Assert.Equal( id, result.Id );
            Assert.Equal( "Type", result.ProductType.Name );
            Assert.Equal( "2", result.ProductType.Properties.ToList()[1].Value );
        }

        /// <summary>
        /// 测试更新 - 先从仓储中查找出来，修改对象属性，直接提交工作单元,该更新方法无效
        /// 原因: 如果使用了Po，从仓储中Find出来的实体只是普通对象，没有被EF跟踪，所以修改属性提交工作单元没有保存更新,必须调用Update方法
        /// </summary>
        [Fact]
        public void TestUpdate_Invalid() {
            int id = _random.Next( 999999999 );
            var product = new Product( id ) { Name = "Name", Code = "Code" };
            _productRepository.Add( product );
            _unitOfWork.Commit();
            _unitOfWork.ClearCache();

            var result = _productRepository.Find( id );
            result.Code = "B";
            _unitOfWork.Commit();

            result = _productRepository.GetById( id );
            Assert.Equal( "Code", result.Code );
        }

        /// <summary>
        /// 测试更新
        /// </summary>
        [Fact]
        public void TestUpdate() {
            int id = _random.Next( 999999999 );
            var product = new Product( id ) { Name = "Name", Code = "Code" };
            _productRepository.Add( product );
            _unitOfWork.Commit();
            _unitOfWork.ClearCache();

            //Find出来修改，必须调用Update方法才能生效
            product = _productRepository.Find( id );
            product.Code = "B";
            _productRepository.Update( product );
            _unitOfWork.Commit();
            var result = _productRepository.GetById( id );
            Assert.Equal( "B", result.Code );
            _unitOfWork.ClearCache();

            //手工定义一个待更新对象,调用Update
            product = new Product( id ) { Name = "Name", Code = "C", Version = result.Version };
            _productRepository.Update( product );
            _unitOfWork.Commit();
            result = _productRepository.GetById( id );
            Assert.Equal( "C", result.Code );
            _unitOfWork.ClearCache();

            //手工定义一个待更新对象,从仓储中Find出原对象，没有影响
            var old = _productRepository.Find( id );
            product = new Product( id ) { Name = "Name", Code = "D", Version = old.Version };
            _productRepository.Update( product );
            _unitOfWork.Commit();
            result = _productRepository.GetById( id );
            Assert.Equal( "D", result.Code );
        }

        /// <summary>
        /// 测试异步更新
        /// </summary>
        [Fact]
        public async Task TestUpdateAsync() {
            int id = _random.Next( 999999999 );
            var product = new Product( id ) { Name = "Name", Code = "Code" };
            await _productRepository.AddAsync( product );
            await _unitOfWork.CommitAsync();
            _unitOfWork.ClearCache();

            //Find出来修改，必须调用Update方法才能生效
            product = await _productRepository.FindAsync( id );
            product.Code = "B";
            await _productRepository.UpdateAsync( product );
            await _unitOfWork.CommitAsync();
            var result = _productRepository.GetById( id );
            Assert.Equal( "B", result.Code );
            _unitOfWork.ClearCache();

            //手工定义一个待更新对象,调用Update
            var old = await _productRepository.FindAsync( id );
            product = new Product( id ) { Name = "Name", Code = "C", Version = old.Version };
            await _productRepository.UpdateAsync( product );
            await _unitOfWork.CommitAsync();
            result = _productRepository.GetById( id );
            Assert.Equal( "C", result.Code );
            _unitOfWork.ClearCache();
        }
    }
}

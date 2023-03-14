import { getCustomRepository } from 'typeorm';
import Product from '../typeorm/entities/Product';
import ProductRepository from '../typeorm/repositories/ProductsRepository';
import RedisCache from '@shared/cache/RedisCache';

export default class ListProductService {
  public async execute(): Promise<Product[]> {
    //instancia o getCustomRepositury productRepository
    const productsRepository = getCustomRepository(ProductRepository);

    //const redisCache = new RedisCache();

    let products = await RedisCache.recover<Product[]>(
      'api-vendas-PRODUCT_LIST',
    );
    //executa o método find da instância do productRepository
    if (!products) {
      products = await productsRepository.find();

      await RedisCache.save('api-vendas-PRODUCT_LIST', products);
    }

    //retorna um array de products
    return products;
  }
}

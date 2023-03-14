import RedisCache from '@shared/cache/RedisCache';
import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import ProductRepository from '../typeorm/repositories/ProductsRepository';

//interface IRequest
interface IRequest {
  id: string;
}

export default class DeleteProductService {
  public async execute({ id }: IRequest): Promise<void> {
    //instancia o getCustomRepositury productRepository
    const productsRepository = getCustomRepository(ProductRepository);

    //executa o método findOne da instância do productRepository
    const product = await productsRepository.findOne(id);

    //verifica se não existe um produto
    //se for true libera a excessão AppError
    if (!product) {
      throw new AppError('Product no found');
    }

    //const redisCache = new RedisCache();

    //LIMPA O CACHE
    await RedisCache.invalidata('api-vendas-PRODUCT_LIST');

    //executa o método remove da instância do productRepository
    await productsRepository.remove(product);
  }
}

import { EntityRepository, In, Repository } from 'typeorm';
import Product from '../entities/Product';

interface IFindProducts {
  id: string;
}

@EntityRepository(Product)
export default class ProductRepository extends Repository<Product> {
  //método findByName porsonalizado do repositório
  public async findByName(name: string): Promise<Product | undefined> {
    const product = this.findOne({
      //filtro
      where: {
        name,
      },
    });
    //retorna um objeto
    return product;
  }

  public async findAllByIds(products: IFindProducts[]): Promise<Product[]> {
    const productIds = products.map(product => product.id);

    const existentProducts = await this.find({
      where: {
        id: In(productIds),
      },
    });

    return existentProducts;
  }
}

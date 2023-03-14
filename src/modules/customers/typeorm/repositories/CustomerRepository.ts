import { EntityRepository, Repository } from 'typeorm';
import Customer from '../entities/Customer';

@EntityRepository(Customer)
export default class CustomerRepository extends Repository<Customer> {
  //método findByName porsonalizado do repositório customer
  public async findByName(name: string): Promise<Customer | undefined> {
    const customer = this.findOne({
      //filtro
      where: {
        name,
      },
    });
    //retorna um objeto
    return customer;
  }

  public async findByEmail(email: string): Promise<Customer | undefined> {
    const customer = this.findOne({
      //filtro
      where: {
        email,
      },
    });
    //retorna um objeto
    return customer;
  }

  public async findById(id: string): Promise<Customer | undefined> {
    const customer = this.findOne({
      //filtro
      where: {
        id,
      },
    });
    //retorna um objeto
    return customer;
  }
}

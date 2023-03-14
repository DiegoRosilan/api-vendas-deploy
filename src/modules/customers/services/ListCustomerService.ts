import { getCustomRepository } from 'typeorm';
import Customer from '../typeorm/entities/Customer';
import CustomerRepository from '../typeorm/repositories/CustomerRepository';

interface IPaginateCustomer {
  from: number;
  to: number;
  per_page: number;
  total: number;
  current_page: number;
  prev_page: number | null;
  next_page: number | null;
  data: Customer[];
}

export default class ListCustomerService {
  public async execute(): Promise<IPaginateCustomer> {
    //instancia o getCustomRepositury CustomerRepository
    const customersRepository = getCustomRepository(CustomerRepository);

    //executa o método find da instância do CustomerRepository
    const customers = await customersRepository.createQueryBuilder().paginate();

    //retorna um array de Customers
    return customers as IPaginateCustomer;
  }
}

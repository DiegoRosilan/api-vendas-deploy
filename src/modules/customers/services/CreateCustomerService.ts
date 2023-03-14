import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Customer from '../typeorm/entities/Customer';
import CustomerRepository from '../typeorm/repositories/CustomerRepository';

//interface IRequest
interface IRequest {
  name: string;
  email: string;
}

export default class CreateCustomerService {
  public async execute({ name, email }: IRequest): Promise<Customer> {
    //instancia o getCustomRepositury CustomerRepository
    const customersRepository = getCustomRepository(CustomerRepository);

    //executa o método findByName da instância do CustomerRepository
    const emailExist = await customersRepository.findByName(email);

    //verifica se já existe um produto como mesmo nome
    //se for true libera a excessão AppError
    if (emailExist) {
      throw new AppError('Email adress already used.');
    }

    //executa o metodo create do respositorio
    const customer = customersRepository.create({
      name,
      email,
    });

    //salva o produto no banco
    await customersRepository.save(customer);

    //retorna um objeto Customer
    return customer;
  }
}

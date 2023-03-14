import OrdersProducts from '@modules/orders/typeorm/entities/OrdersProducts';
import {
  Column,
  CreateDateColumn,
  Entity,
  OneToMany,
  PrimaryGeneratedColumn,
  UpdateDateColumn,
} from 'typeorm';

//determina o entidade do banco de dados ligada a classe
@Entity('products')
export default class Product {
  //atributos da classe com seus respectivos tipos
  @PrimaryGeneratedColumn('uuid')
  id: string;

  //relaciona um product com muitas OrdersProducts
  //onde order_products se relaciona com product na Entity OrdersProducts
  @OneToMany(() => OrdersProducts, order_products => order_products.product)
  order_products: OrdersProducts[];

  @Column()
  name: string;

  @Column('decimal')
  price: number;

  @Column('int')
  quantity: number;

  @CreateDateColumn()
  created_at: Date;

  @UpdateDateColumn()
  updated_at: Date;
}
